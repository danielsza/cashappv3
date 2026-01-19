# Cash Drawer System - Deployment Checklist

Use this checklist when deploying the system to ensure nothing is missed.

## 📋 Pre-Deployment

### Hardware Preparation

- [ ] USB-to-Serial adapters purchased (1 per server)
- [ ] Relay modules connected to adapters
- [ ] Cash drawer connected to relay
- [ ] Test relay with multimeter (if possible)

### Network Preparation

- [ ] Static IP addresses assigned for both servers (recommended)
- [ ] Port 5000 opened in Windows Firewall on servers
- [ ] Network share `\\partsrv2\Parts\Cash` accessible
- [ ] Write permissions verified on network share
- [ ] Document server IP addresses:
  - Server 1: `___.___.___.___ `
  - Server 2: `___.___.___.___ `

### Software Preparation

- [ ] Python 3.8+ downloaded for all computers
- [ ] CashApp files copied to deployment USB drive
- [ ] Backup of old system (if any)

---

## 🖥️ Server 1 Deployment

### Installation

- [ ] Python installed with "Add to PATH" checked
- [ ] `pip install pyserial` completed successfully
- [ ] Files copied to `C:\CashApp\Server\`
- [ ] USB adapter connected

### COM Port Setup

- [ ] Device Manager opened
- [ ] USB Serial device located
- [ ] COM port changed to COM10 (or documented alternative: `_____`)
- [ ] Computer rebooted
- [ ] COM10 verified in Device Manager after reboot

### Configuration

- [ ] First run completed: `python CashServer.py`
- [ ] `server_config.ini` edited:
  - [ ] ServerID = `SERVER1`
  - [ ] Port = `5000`
  - [ ] PeerServerHost = `[Server 2 IP]`
  - [ ] COMPort = `COM10` (or your port)
  - [ ] RelayPin = `DTR` or `RTS`
  - [ ] LogPath = `\\partsrv2\Parts\Cash`

### User Setup

- [ ] `UserManager.py` run
- [ ] Admin password changed from default
- [ ] User accounts created:
  - [ ] User: `____________` (Name: `____________`)
  - [ ] User: `____________` (Name: `____________`)
  - [ ] User: `____________` (Name: `____________`)

### Testing

- [ ] Server started: `python CashServer.py`
- [ ] "Serial port COM10 initialized successfully" message seen
- [ ] No errors in console
- [ ] Server keeps running
- [ ] Log file created in `C:\CashApp\Server\logs\`

### Hardware Test

- [ ] Test command manually (if possible)
- [ ] Relay clicks/activates
- [ ] Cash drawer opens
- [ ] Transaction logged

---

## 🖥️ Server 2 Deployment

### Installation

- [ ] Python installed with "Add to PATH" checked
- [ ] `pip install pyserial` completed successfully
- [ ] Files copied to `C:\CashApp\Server\`
- [ ] USB adapter connected

### COM Port Setup

- [ ] Device Manager opened
- [ ] USB Serial device located
- [ ] COM port changed to COM10 (or documented alternative: `_____`)
- [ ] Computer rebooted
- [ ] COM10 verified in Device Manager after reboot

### Configuration

- [ ] First run completed: `python CashServer.py`
- [ ] `server_config.ini` edited:
  - [ ] ServerID = `SERVER2`
  - [ ] Port = `5000`
  - [ ] PeerServerHost = `[Server 1 IP]`
  - [ ] COMPort = `COM10` (or your port)
  - [ ] RelayPin = `DTR` or `RTS`
  - [ ] LogPath = `\\partsrv2\Parts\Cash`

### User Setup

- [ ] `users.json` copied from Server 1 (to keep same users)
  - OR
- [ ] `UserManager.py` run to create users manually
- [ ] Verify users match Server 1

### Testing

- [ ] Server started: `python CashServer.py`
- [ ] "Serial port COM10 initialized successfully" message seen
- [ ] No errors in console
- [ ] Server keeps running
- [ ] Log file created in `C:\CashApp\Server\logs\`
- [ ] Peer heartbeat shows Server 1 online (check logs)

### Hardware Test

- [ ] Test command manually
- [ ] Relay clicks/activates
- [ ] Cash drawer opens
- [ ] Transaction logged

---

## 💻 Client Computers Deployment

Repeat for each client computer:

### Client Computer: `______________`

#### Installation

- [ ] Python installed
- [ ] Files copied to `C:\CashApp\Client\`

#### Configuration

- [ ] `CashClient.py` run first time
- [ ] Settings configured:
  - [ ] Primary Server: `[Server 1 IP]`
  - [ ] Primary Port: `5000`
  - [ ] Secondary Server: `[Server 2 IP]`
  - [ ] Secondary Port: `5000`
  - [ ] Auto-connect: `☑` (if desired)
  - [ ] Remember username: `☑`
- [ ] Settings saved

#### Testing

- [ ] "Test Connection" clicked
- [ ] Both servers show ✓ Connected
- [ ] Login successful with test user
- [ ] Control tab accessible
- [ ] Test transaction entered
- [ ] "Open" button clicked
- [ ] Drawer opens
- [ ] Transaction appears in server logs

#### Shortcuts

- [ ] Desktop shortcut created: `StartClient.bat`
- [ ] Shortcut tested

---

## 🎯 Final Verification

### System Integration Test

- [ ] All servers running simultaneously
- [ ] Multiple clients connected at once
- [ ] Transactions from different clients successful
- [ ] Logs writing to network share
- [ ] Server 1 logs show peer status
- [ ] Server 2 logs show peer status

### Failover Test

- [ ] Client connected to Server 1
- [ ] Server 1 stopped
- [ ] Client automatically connects to Server 2
- [ ] Transaction successful on Server 2
- [ ] Server 1 restarted
- [ ] Client can connect to Server 1 again

### User Management Test

- [ ] New user added via UserManager
- [ ] New user can login
- [ ] Wrong password locks account after 3 tries
- [ ] Account unlocked via UserManager
- [ ] User can login again
- [ ] Password changed successfully

### Security Test

- [ ] Failed login attempts logged
- [ ] Account lockout working
- [ ] Unauthorized user message appears
- [ ] Logs show failed attempts

### Log Verification

- [ ] Network logs created: `\\partsrv2\Parts\Cash\`
- [ ] Local logs created: `C:\CashApp\Server\logs\`
- [ ] Transaction log format correct
- [ ] Timestamps accurate
- [ ] All required data present

---

## 🚀 Production Readiness

### Documentation

- [ ] Server IP addresses documented
- [ ] COM port numbers documented
- [ ] User accounts documented (usernames, not passwords!)
- [ ] Network share path verified
- [ ] Emergency contacts listed

### Training

- [ ] Staff trained on login procedure
- [ ] Staff trained on cash drawer operation
- [ ] Staff trained on BOD/EOD procedures
- [ ] Admin trained on user management
- [ ] Admin trained on server monitoring

### Auto-Start Setup

#### Server 1

- [ ] Startup folder shortcut created
  - OR
- [ ] Task Scheduler task created
- [ ] Auto-start tested after reboot

#### Server 2

- [ ] Startup folder shortcut created
  - OR
- [ ] Task Scheduler task created
- [ ] Auto-start tested after reboot

### Monitoring

- [ ] Log review schedule established
- [ ] Server monitoring procedure documented
- [ ] Backup procedure documented
- [ ] Troubleshooting contacts listed

---

## 📝 Post-Deployment

### Day 1

- [ ] Monitor logs for errors
- [ ] Check all clients connecting successfully
- [ ] Verify transactions logging correctly
- [ ] Address any issues immediately

### Week 1

- [ ] Review transaction logs
- [ ] Check for authentication issues
- [ ] Verify network logging working
- [ ] Collect user feedback

### Month 1

- [ ] Review system performance
- [ ] Check disk space for logs
- [ ] Archive old logs if needed
- [ ] Update documentation based on experience

---

## ⚠️ Emergency Contacts

**IT Support**: `____________` Phone: `____________`

**Server Location**: `____________`

**Network Admin**: `____________` Phone: `____________`

---

## 📌 Notes

Additional notes or observations during deployment:

```
_______________________________________________________________

_______________________________________________________________

_______________________________________________________________

_______________________________________________________________
```

---

## ✅ Sign-Off

Deployment completed by: `____________` Date: `____________`

Verified by: `____________` Date: `____________`

System Status: ☐ Production Ready   ☐ Issues to Resolve

Issues to resolve:
```
_______________________________________________________________

_______________________________________________________________
```
