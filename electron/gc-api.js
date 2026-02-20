/**
 * GlobalConnect API Module
 * Direct REST API access to pwbplus.vsp.autopartners.net
 *
 * Auth: Azure AD OAuth2 with interactive browser login
 * Endpoints:
 *   GET /api/rest/v1/orders/shipments?customerCode=X&fromDate=Y&toDate=Z
 *   GET /api/rest/v1/orders/answerbacks?customerCode=X&fromDate=Y&toDate=Z
 */

const { BrowserWindow } = require("electron");
const https = require("https");
const crypto = require("crypto");

// Azure AD / GM config
const TENANT_ID = "5de110f8-2e0f-4d45-891d-bcf2218e253d";
const CLIENT_ID = "e5571be0-a422-45b0-91e0-1c6680a6e403";
const REDIRECT_URI = "https://pwbplus.vsp.autopartners.net";
const AUTH_BASE = `https://login.microsoftonline.com/${TENANT_ID}/oauth2/v2.0`;
const API_BASE = "https://pwbplus.vsp.autopartners.net";
const SCOPE = `api://${CLIENT_ID}/.default openid profile offline_access`;

// Token state
let currentToken = null;
let tokenExpiry = 0;
let refreshToken = null;

// ─── PKCE helpers ────────────────────────────────────────────────

function base64URLEncode(buffer) {
  return buffer.toString("base64").replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
}

function generateCodeVerifier() {
  return base64URLEncode(crypto.randomBytes(32));
}

function generateCodeChallenge(verifier) {
  return base64URLEncode(crypto.createHash("sha256").update(verifier).digest());
}

// ─── Interactive Login ──────────────────────────────────────────

/**
 * Open a browser window for Azure AD login, capture the auth code,
 * exchange it for an access token.
 */
async function interactiveLogin(parentWindow) {
  const codeVerifier = generateCodeVerifier();
  const codeChallenge = generateCodeChallenge(codeVerifier);
  const state = crypto.randomBytes(16).toString("hex");
  const nonce = crypto.randomUUID();

  const authUrl = `${AUTH_BASE}/authorize?` + new URLSearchParams({
    client_id: CLIENT_ID,
    response_type: "code",
    redirect_uri: REDIRECT_URI,
    scope: SCOPE,
    state: state,
    nonce: nonce,
    code_challenge: codeChallenge,
    code_challenge_method: "S256",
    prompt: "select_account",
  }).toString();

  return new Promise((resolve, reject) => {
    const authWindow = new BrowserWindow({
      width: 520,
      height: 700,
      parent: parentWindow,
      modal: true,
      show: true,
      webPreferences: {
        nodeIntegration: false,
        contextIsolation: true,
      },
      title: "Sign in to GlobalConnect",
    });

    authWindow.setMenuBarVisibility(false);

    let codeHandled = false;

    // Watch for redirect with auth code
    const handleNavigation = async (event, url) => {
      if (codeHandled) return;
      try {
        const parsed = new URL(url);
        // After login, Azure redirects back to REDIRECT_URI with ?code=...
        if (parsed.origin === new URL(REDIRECT_URI).origin && parsed.searchParams.has("code")) {
          codeHandled = true;
          // Stop the redirect from loading PWB+ app (race condition with SPA's MSAL)
          if (event && event.preventDefault) event.preventDefault();

          const code = parsed.searchParams.get("code");
          const returnedState = parsed.searchParams.get("state");

          if (returnedState !== state) {
            reject(new Error("State mismatch — possible CSRF"));
            authWindow.close();
            return;
          }

          // Show loading state
          authWindow.loadURL("data:text/html,<html><body style='display:flex;align-items:center;justify-content:center;height:100vh;font-family:system-ui;color:%23666'><div style='text-align:center'><div style='font-size:32px;margin-bottom:12px'>🔐</div><div>Signing in...</div></div></body></html>");

          // Exchange code for tokens
          try {
            const tokens = await exchangeCodeForToken(code, codeVerifier);
            currentToken = tokens.access_token;
            refreshToken = tokens.refresh_token || null;
            tokenExpiry = Date.now() + (tokens.expires_in - 60) * 1000; // 60s buffer
            authWindow.close();
            resolve({
              success: true,
              expiresIn: tokens.expires_in,
              user: parseTokenUser(tokens.access_token),
            });
          } catch (err) {
            authWindow.close();
            reject(err);
          }
        }
      } catch (e) {
        // Not a valid URL or not our redirect, ignore
      }
    };

    authWindow.webContents.on("will-redirect", (event, url) => handleNavigation(event, url));
    authWindow.webContents.on("will-navigate", (event, url) => handleNavigation(event, url));
    // Also check after page loads (some flows don't trigger will-redirect)
    authWindow.webContents.on("did-navigate", (event, url) => handleNavigation(null, url));

    authWindow.on("closed", () => {
      if (!codeHandled) {
        reject(new Error("Login window closed without completing authentication"));
      }
    });

    authWindow.loadURL(authUrl);
  });
}

// ─── Token Exchange ─────────────────────────────────────────────

function exchangeCodeForToken(code, codeVerifier) {
  return new Promise((resolve, reject) => {
    const body = new URLSearchParams({
      client_id: CLIENT_ID,
      grant_type: "authorization_code",
      code: code,
      redirect_uri: REDIRECT_URI,
      code_verifier: codeVerifier,
      scope: SCOPE,
    }).toString();

    const req = https.request(`${AUTH_BASE}/token`, {
      method: "POST",
      headers: {
        "Content-Type": "application/x-www-form-urlencoded",
        "Content-Length": Buffer.byteLength(body),
      },
    }, (res) => {
      let data = "";
      res.on("data", (chunk) => data += chunk);
      res.on("end", () => {
        try {
          const json = JSON.parse(data);
          if (json.access_token) resolve(json);
          else reject(new Error(json.error_description || json.error || "Token exchange failed"));
        } catch (e) { reject(e); }
      });
    });
    req.on("error", reject);
    req.write(body);
    req.end();
  });
}

// ─── Token Refresh ──────────────────────────────────────────────

async function refreshAccessToken() {
  if (!refreshToken) throw new Error("No refresh token available — please login again");

  return new Promise((resolve, reject) => {
    const body = new URLSearchParams({
      client_id: CLIENT_ID,
      grant_type: "refresh_token",
      refresh_token: refreshToken,
      scope: SCOPE,
    }).toString();

    const req = https.request(`${AUTH_BASE}/token`, {
      method: "POST",
      headers: {
        "Content-Type": "application/x-www-form-urlencoded",
        "Content-Length": Buffer.byteLength(body),
      },
    }, (res) => {
      let data = "";
      res.on("data", (chunk) => data += chunk);
      res.on("end", () => {
        try {
          const json = JSON.parse(data);
          if (json.access_token) {
            currentToken = json.access_token;
            refreshToken = json.refresh_token || refreshToken;
            tokenExpiry = Date.now() + (json.expires_in - 60) * 1000;
            resolve({ success: true, expiresIn: json.expires_in });
          } else {
            // Refresh token expired — need fresh login
            currentToken = null;
            refreshToken = null;
            tokenExpiry = 0;
            reject(new Error("Refresh token expired — please login again"));
          }
        } catch (e) { reject(e); }
      });
    });
    req.on("error", reject);
    req.write(body);
    req.end();
  });
}

// ─── Ensure Valid Token ─────────────────────────────────────────

async function ensureToken(parentWindow) {
  // Token still valid
  if (currentToken && Date.now() < tokenExpiry) {
    return currentToken;
  }
  // Try refresh
  if (refreshToken) {
    try {
      await refreshAccessToken();
      return currentToken;
    } catch (e) {
      // Refresh failed, fall through to interactive login
    }
  }
  // Need interactive login
  await interactiveLogin(parentWindow);
  return currentToken;
}

// ─── API Calls ──────────────────────────────────────────────────

function apiGet(endpoint, params) {
  return new Promise((resolve, reject) => {
    if (!currentToken) return reject(new Error("Not authenticated"));

    const url = `${API_BASE}${endpoint}?${new URLSearchParams(params).toString()}`;
    const parsed = new URL(url);

    const req = https.request({
      hostname: parsed.hostname,
      port: 443,
      path: parsed.pathname + parsed.search,
      method: "GET",
      headers: {
        "Accept": "application/json",
        "Authorization": `Bearer ${currentToken}`,
        "User-Agent": "GMPartsReceiving/1.0",
      },
    }, (res) => {
      let data = "";
      res.on("data", (chunk) => data += chunk);
      res.on("end", () => {
        if (res.statusCode === 401) {
          // Token expired mid-request
          currentToken = null;
          tokenExpiry = 0;
          reject(new Error("Token expired — please login again"));
          return;
        }
        if (res.statusCode !== 200) {
          reject(new Error(`API returned ${res.statusCode}: ${data.substring(0, 200)}`));
          return;
        }
        try {
          resolve(JSON.parse(data));
        } catch (e) { reject(new Error(`Invalid JSON response: ${e.message}`)); }
      });
    });
    req.on("error", reject);
    req.end();
  });
}

/**
 * Fetch shipments for a date range
 */
async function fetchShipments(customerCode, fromDate, toDate) {
  return apiGet("/api/rest/v1/orders/shipments", { customerCode, fromDate, toDate });
}

/**
 * Fetch answerbacks for a date range
 */
async function fetchAnswerbacks(customerCode, fromDate, toDate) {
  return apiGet("/api/rest/v1/orders/answerbacks", { customerCode, fromDate, toDate });
}

// ─── Helpers ────────────────────────────────────────────────────

function parseTokenUser(token) {
  try {
    const payload = JSON.parse(Buffer.from(token.split(".")[1], "base64").toString());
    return {
      name: payload.name || "",
      email: payload.gmVSSMemail || payload.preferred_username || "",
      userId: payload.gmVSSMUserID || "",
      role: payload.gmVSSMBusinessRole || "",
      partnerType: payload.gmVSSMPartnerType || "",
    };
  } catch (e) { return null; }
}

function getTokenStatus() {
  if (!currentToken) return { authenticated: false };
  const remaining = Math.max(0, Math.floor((tokenExpiry - Date.now()) / 1000));
  return {
    authenticated: true,
    expiresIn: remaining,
    hasRefreshToken: !!refreshToken,
    user: parseTokenUser(currentToken),
  };
}

function logout() {
  currentToken = null;
  refreshToken = null;
  tokenExpiry = 0;
}

module.exports = {
  interactiveLogin,
  ensureToken,
  fetchShipments,
  fetchAnswerbacks,
  getTokenStatus,
  logout,
};
