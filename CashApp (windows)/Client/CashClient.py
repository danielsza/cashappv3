"""
Cash Drawer Client Application - GUI
Cross-platform client for cash drawer control (Windows, macOS, Linux)
"""

import tkinter as tk
from tkinter import ttk, messagebox
import socket
import json
import configparser
import os
import platform
from datetime import datetime
import threading
import math
import struct

# Platform detection
IS_WINDOWS = platform.system() == 'Windows'
IS_MACOS = platform.system() == 'Darwin'
IS_LINUX = platform.system() == 'Linux'

class CashClient:
    def __init__(self):
        self.root = tk.Tk()
        self.root.title("Open Cash")
        self.root.geometry("400x550")
        self.root.resizable(False, False)
        
        # Configuration
        self.config = configparser.ConfigParser()
        self.config_file = 'client_config.ini'
        self.load_config()
        
        # Session state
        self.authenticated = False
        self.session_token = None
        self.username = None
        self.user_info = None
        self.current_server = None
        
        # Create GUI
        self.create_widgets()
        
        # Auto-connect if configured
        if self.config.get('Client', 'AutoConnect', fallback='false').lower() == 'true':
            self.root.after(100, self.connect_to_server)
    
    def canadian_penny_rounding(self, amount):
        """
        Apply Canadian penny rounding rules for cash transactions.
        
        Fair Rounding Rule:
        * Ends in .01 or .02: Rounds down to .00
        * Ends in .03 or .04: Rounds up to .05
        * Ends in .06 or .07: Rounds down to .05
        * Ends in .08 or .09: Rounds up to .10
        """
        # Get the cents portion
        cents = round((amount - math.floor(amount)) * 100)
        dollars = math.floor(amount)
        
        # Apply rounding rules
        if cents in [1, 2]:
            rounded_cents = 0
        elif cents in [3, 4]:
            rounded_cents = 5
        elif cents in [6, 7]:
            rounded_cents = 5
        elif cents in [8, 9]:
            rounded_cents = 10
        else:
            # 0, 5, or values that round to 10+ stay as is
            rounded_cents = cents
        
        # Handle the case where cents rounds to 10
        if rounded_cents >= 10:
            return dollars + (rounded_cents / 100.0)
        else:
            return dollars + (rounded_cents / 100.0)
    
    def discover_servers(self, timeout=3):
        """
        Discover cash drawer servers on the local network using UDP broadcast.
        Returns list of (ip, port, server_id) tuples.
        """
        discovered = []
        
        try:
            # Create UDP socket for broadcast
            sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
            sock.setsockopt(socket.SOL_SOCKET, socket.SO_BROADCAST, 1)
            sock.settimeout(timeout)
            
            # Send discovery broadcast on port 5001 (discovery port)
            discovery_msg = json.dumps({'command': 'discover', 'type': 'cash_client'}).encode()
            sock.sendto(discovery_msg, ('<broadcast>', 5001))
            
            # Listen for responses
            start_time = datetime.now()
            while (datetime.now() - start_time).seconds < timeout:
                try:
                    data, addr = sock.recvfrom(1024)
                    response = json.loads(data.decode())
                    
                    if response.get('type') == 'cash_server':
                        server_info = (
                            addr[0],  # IP address
                            response.get('port', 5000),  # Server port
                            response.get('server_id', 'Unknown')  # Server ID
                        )
                        
                        # Avoid duplicates
                        if server_info not in discovered:
                            discovered.append(server_info)
                            
                except socket.timeout:
                    break
                except Exception:
                    continue
            
            sock.close()
            
        except Exception as e:
            print(f"Discovery error: {e}")
        
        return discovered
    
    def scan_local_network(self):
        """
        Scan common IP addresses on local network for servers.
        Faster than full network scan, checks likely candidates.
        """
        discovered = []
        
        # Get local IP to determine subnet
        try:
            s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
            s.connect(("8.8.8.8", 80))
            local_ip = s.getsockname()[0]
            s.close()
            
            # Extract subnet (e.g., 192.168.1.x)
            ip_parts = local_ip.split('.')
            subnet = '.'.join(ip_parts[:3])
            
            # Common server IPs to check (customize for your network)
            common_ips = [
                f"{subnet}.10",   # Often used for servers
                f"{subnet}.11",
                f"{subnet}.20",
                f"{subnet}.21",
                f"{subnet}.100",
                f"{subnet}.101",
                f"{subnet}.200",
                f"{subnet}.201",
            ]
            
            # Add broadcast discovery results
            broadcast_results = self.discover_servers(timeout=2)
            for ip, port, server_id in broadcast_results:
                discovered.append((ip, port, server_id))
            
            # Quick TCP scan of common IPs
            for ip in common_ips:
                try:
                    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
                    sock.settimeout(0.5)  # Very quick timeout
                    result = sock.connect_ex((ip, 5000))
                    
                    if result == 0:
                        # Port is open, verify it's our server
                        try:
                            sock.send(json.dumps({'command': 'ping'}).encode())
                            response = json.loads(sock.recv(1024).decode())
                            
                            if response.get('status') == 'success':
                                server_id = response.get('server_id', 'Unknown')
                                server_info = (ip, 5000, server_id)
                                
                                # Avoid duplicates
                                if server_info not in discovered:
                                    discovered.append(server_info)
                        except:
                            pass
                    
                    sock.close()
                except:
                    continue
                    
        except Exception as e:
            print(f"Network scan error: {e}")
        
        return discovered
    
    def load_config(self):
        """Load or create configuration"""
        if os.path.exists(self.config_file):
            self.config.read(self.config_file)
        else:
            self.config['Client'] = {
                'PrimaryServer': 'localhost',
                'PrimaryPort': '5000',
                'SecondaryServer': '',
                'SecondaryPort': '5000',
                'AutoConnect': 'false',
                'RememberUsername': 'true',
                'LastUsername': ''
            }
            
            with open(self.config_file, 'w') as f:
                self.config.write(f)
    
    def save_config(self):
        """Save configuration"""
        with open(self.config_file, 'w') as f:
            self.config.write(f)
    
    def create_widgets(self):
        """Create GUI widgets"""
        # Title/Status Bar
        self.status_frame = ttk.Frame(self.root)
        self.status_frame.pack(fill=tk.X, padx=10, pady=5)
        
        self.status_label = ttk.Label(
            self.status_frame,
            text="Not Connected",
            font=('Arial', 10, 'bold'),
            foreground='red'
        )
        self.status_label.pack(side=tk.LEFT)
        
        self.server_label = ttk.Label(
            self.status_frame,
            text="",
            font=('Arial', 8)
        )
        self.server_label.pack(side=tk.RIGHT)
        
        # Notebook for tabs
        self.notebook = ttk.Notebook(self.root)
        self.notebook.pack(fill=tk.BOTH, expand=True, padx=10, pady=5)
        
        # Login Tab
        self.login_frame = ttk.Frame(self.notebook)
        self.notebook.add(self.login_frame, text="Login")
        self.create_login_tab()
        
        # Control Tab
        self.control_frame = ttk.Frame(self.notebook)
        self.notebook.add(self.control_frame, text="Control")
        self.create_control_tab()
        
        # Settings Tab
        self.settings_frame = ttk.Frame(self.notebook)
        self.notebook.add(self.settings_frame, text="Settings")
        self.create_settings_tab()
        
        # Initially disable control tab
        self.notebook.tab(1, state='disabled')
        
        # Status bar at bottom
        self.bottom_status = ttk.Label(
            self.root,
            text="Ready",
            relief=tk.SUNKEN,
            anchor=tk.W
        )
        self.bottom_status.pack(fill=tk.X, side=tk.BOTTOM)
    
    def create_login_tab(self):
        """Create login interface"""
        frame = ttk.LabelFrame(self.login_frame, text="User Authentication", padding=20)
        frame.pack(fill=tk.BOTH, expand=True, padx=20, pady=20)
        
        # Username
        ttk.Label(frame, text="Username:").grid(row=0, column=0, sticky=tk.W, pady=5)
        self.username_entry = ttk.Entry(frame, width=30)
        self.username_entry.grid(row=0, column=1, pady=5)
        
        # Pre-fill last username
        last_user = self.config.get('Client', 'LastUsername', fallback='')
        if last_user:
            self.username_entry.insert(0, last_user)
        
        # Password
        ttk.Label(frame, text="Password:").grid(row=1, column=0, sticky=tk.W, pady=5)
        self.password_entry = ttk.Entry(frame, width=30, show="*")
        self.password_entry.grid(row=1, column=1, pady=5)
        
        # Bind Enter key
        self.username_entry.bind('<Return>', lambda e: self.password_entry.focus())
        self.password_entry.bind('<Return>', lambda e: self.login())
        
        # Login button
        self.login_button = ttk.Button(
            frame,
            text="Login",
            command=self.login,
            width=20
        )
        self.login_button.grid(row=2, column=0, columnspan=2, pady=20)
        
        # User info display (hidden initially)
        self.user_info_label = ttk.Label(
            frame,
            text="",
            font=('Arial', 9)
        )
        self.user_info_label.grid(row=3, column=0, columnspan=2)
    
    def create_control_tab(self):
        """Create cash drawer control interface"""
        # Document ID Section
        doc_frame = ttk.LabelFrame(self.control_frame, text="Document ID", padding=15)
        doc_frame.pack(fill=tk.X, padx=20, pady=10)
        
        self.doc_vars = {}
        doc_types = ["Invoice", "Petty Cash", "Change", "Refund", "BOD", "EOD"]
        
        for doc_type in doc_types:
            var = tk.BooleanVar()
            cb = ttk.Checkbutton(doc_frame, text=doc_type, variable=var)
            cb.pack(anchor=tk.W)
            self.doc_vars[doc_type] = var
        
        # Transaction Details
        trans_frame = ttk.LabelFrame(self.control_frame, text="Transaction Details", padding=15)
        trans_frame.pack(fill=tk.X, padx=20, pady=10)
        
        # Total
        ttk.Label(trans_frame, text="Total:").grid(row=0, column=0, sticky=tk.W, pady=3)
        self.total_entry = ttk.Entry(trans_frame, width=20)
        self.total_entry.grid(row=0, column=1, pady=3)
        self.total_entry.insert(0, "0.00")
        
        # IN
        ttk.Label(trans_frame, text="IN:").grid(row=1, column=0, sticky=tk.W, pady=3)
        self.in_entry = ttk.Entry(trans_frame, width=20)
        self.in_entry.grid(row=1, column=1, pady=3)
        self.in_entry.insert(0, "0.00")
        
        # Out
        ttk.Label(trans_frame, text="Out:").grid(row=2, column=0, sticky=tk.W, pady=3)
        self.out_entry = ttk.Entry(trans_frame, width=20)
        self.out_entry.grid(row=2, column=1, pady=3)
        self.out_entry.insert(0, "0.00")
        
        # Penny rounding indicator
        self.rounding_label = ttk.Label(
            trans_frame,
            text="🍁 Canadian penny rounding applied",
            font=('Arial', 8),
            foreground='#666666'
        )
        self.rounding_label.grid(row=3, column=0, columnspan=2, pady=(5, 0))
        
        # Auto-calculate change
        self.in_entry.bind('<KeyRelease>', self.calculate_change)
        self.total_entry.bind('<KeyRelease>', self.calculate_change)
        
        # Open button
        self.open_button = ttk.Button(
            self.control_frame,
            text="Open",
            command=self.open_drawer,
            width=20,
            style='Accent.TButton'
        )
        self.open_button.pack(pady=20)
        
        # Quick actions frame
        quick_frame = ttk.LabelFrame(self.control_frame, text="Quick Actions", padding=10)
        quick_frame.pack(fill=tk.X, padx=20, pady=10)
        
        ttk.Button(quick_frame, text="BOD", command=lambda: self.quick_open("BOD"), width=15).pack(side=tk.LEFT, padx=5)
        ttk.Button(quick_frame, text="EOD", command=lambda: self.quick_open("EOD"), width=15).pack(side=tk.LEFT, padx=5)
    
    def create_settings_tab(self):
        """Create settings interface"""
        frame = ttk.Frame(self.settings_frame, padding=20)
        frame.pack(fill=tk.BOTH, expand=True)
        
        # Server settings
        server_frame = ttk.LabelFrame(frame, text="Server Configuration", padding=15)
        server_frame.pack(fill=tk.X, pady=10)
        
        ttk.Label(server_frame, text="Primary Server:").grid(row=0, column=0, sticky=tk.W, pady=3)
        self.primary_server_entry = ttk.Entry(server_frame, width=25)
        self.primary_server_entry.grid(row=0, column=1, pady=3)
        self.primary_server_entry.insert(0, self.config.get('Client', 'PrimaryServer'))
        
        ttk.Label(server_frame, text="Primary Port:").grid(row=1, column=0, sticky=tk.W, pady=3)
        self.primary_port_entry = ttk.Entry(server_frame, width=25)
        self.primary_port_entry.grid(row=1, column=1, pady=3)
        self.primary_port_entry.insert(0, self.config.get('Client', 'PrimaryPort'))
        
        ttk.Label(server_frame, text="Secondary Server:").grid(row=2, column=0, sticky=tk.W, pady=3)
        self.secondary_server_entry = ttk.Entry(server_frame, width=25)
        self.secondary_server_entry.grid(row=2, column=1, pady=3)
        self.secondary_server_entry.insert(0, self.config.get('Client', 'SecondaryServer'))
        
        ttk.Label(server_frame, text="Secondary Port:").grid(row=3, column=0, sticky=tk.W, pady=3)
        self.secondary_port_entry = ttk.Entry(server_frame, width=25)
        self.secondary_port_entry.grid(row=3, column=1, pady=3)
        self.secondary_port_entry.insert(0, self.config.get('Client', 'SecondaryPort'))
        
        # Options
        options_frame = ttk.LabelFrame(frame, text="Options", padding=15)
        options_frame.pack(fill=tk.X, pady=10)
        
        self.auto_connect_var = tk.BooleanVar(
            value=self.config.get('Client', 'AutoConnect', fallback='false').lower() == 'true'
        )
        ttk.Checkbutton(
            options_frame,
            text="Auto-connect on startup",
            variable=self.auto_connect_var
        ).pack(anchor=tk.W)
        
        self.remember_user_var = tk.BooleanVar(
            value=self.config.get('Client', 'RememberUsername', fallback='true').lower() == 'true'
        )
        ttk.Checkbutton(
            options_frame,
            text="Remember username",
            variable=self.remember_user_var
        ).pack(anchor=tk.W)
        
        # Save button
        ttk.Button(
            frame,
            text="Save Settings",
            command=self.save_settings,
            width=20
        ).pack(pady=20)
        
        # Discover servers button
        ttk.Button(
            frame,
            text="🔍 Discover Servers",
            command=self.discover_and_display_servers,
            width=20
        ).pack(pady=5)
        
        # Connection test
        ttk.Button(
            frame,
            text="Test Connection",
            command=self.test_connection,
            width=20
        ).pack()
    
    def calculate_change(self, event=None):
        """Auto-calculate change with Canadian penny rounding"""
        try:
            total = float(self.total_entry.get() or 0)
            amount_in = float(self.in_entry.get() or 0)
            
            # Calculate raw change
            change = amount_in - total
            
            # Apply Canadian penny rounding to the change
            rounded_change = self.canadian_penny_rounding(change)
            
            self.out_entry.delete(0, tk.END)
            self.out_entry.insert(0, f"{rounded_change:.2f}")
        except ValueError:
            pass
    
    def connect_to_server(self):
        """Connect to primary or secondary server, with auto-discovery fallback"""
        servers = []
        
        # Add configured servers first
        primary = self.config.get('Client', 'PrimaryServer')
        secondary = self.config.get('Client', 'SecondaryServer')
        
        if primary:
            servers.append((primary, int(self.config.get('Client', 'PrimaryPort'))))
        if secondary:
            servers.append((secondary, int(self.config.get('Client', 'SecondaryPort'))))
        
        # If no servers configured, try discovery
        if not servers:
            self.bottom_status.config(text="No servers configured, searching network...")
            discovered = self.scan_local_network()
            
            if discovered:
                self.bottom_status.config(text=f"Found {len(discovered)} server(s) on network")
                
                # Show discovery results to user
                result = messagebox.askyesno(
                    "Servers Found",
                    f"Found {len(discovered)} server(s):\n\n" +
                    "\n".join([f"  • {sid} at {ip}:{port}" for ip, port, sid in discovered]) +
                    "\n\nWould you like to use the first server and save it?",
                    icon='question'
                )
                
                if result and discovered:
                    # Use first discovered server
                    ip, port, server_id = discovered[0]
                    servers.append((ip, port))
                    
                    # Offer to save to config
                    self.config.set('Client', 'PrimaryServer', ip)
                    self.config.set('Client', 'PrimaryPort', str(port))
                    if len(discovered) > 1:
                        ip2, port2, sid2 = discovered[1]
                        self.config.set('Client', 'SecondaryServer', ip2)
                        self.config.set('Client', 'SecondaryPort', str(port2))
                    self.save_config()
                    
                    # Update settings UI if visible
                    if hasattr(self, 'primary_server_entry'):
                        self.primary_server_entry.delete(0, tk.END)
                        self.primary_server_entry.insert(0, ip)
                        self.primary_port_entry.delete(0, tk.END)
                        self.primary_port_entry.insert(0, str(port))
                        
                        if len(discovered) > 1:
                            self.secondary_server_entry.delete(0, tk.END)
                            self.secondary_server_entry.insert(0, ip2)
                            self.secondary_port_entry.delete(0, tk.END)
                            self.secondary_port_entry.insert(0, str(port2))
            else:
                messagebox.showerror(
                    "No Servers Found",
                    "Could not find any cash drawer servers on the network.\n\n"
                    "Please configure server addresses manually in Settings."
                )
                return False
        
        # Try each server
        for host, port in servers:
            if not host:
                continue
            
            try:
                # Test connection
                sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
                sock.settimeout(3)
                sock.connect((host, port))
                sock.close()
                
                self.current_server = (host, port)
                self.status_label.config(text="Connected", foreground='green')
                self.server_label.config(text=f"{host}:{port}")
                self.bottom_status.config(text=f"Connected to {host}:{port}")
                return True
                
            except Exception as e:
                continue
        
        self.status_label.config(text="Connection Failed", foreground='red')
        self.bottom_status.config(text="Unable to connect to any server")
        messagebox.showerror("Connection Error", "Could not connect to any server. Please check settings.")
        return False
    
    def send_command(self, command):
        """Send command to server"""
        if not self.current_server:
            if not self.connect_to_server():
                return None
        
        try:
            sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            sock.settimeout(10)
            sock.connect(self.current_server)
            
            sock.send(json.dumps(command).encode())
            response = json.loads(sock.recv(4096).decode())
            
            sock.close()
            return response
            
        except Exception as e:
            # Try secondary server
            if self.current_server == (self.config.get('Client', 'PrimaryServer'), 
                                       int(self.config.get('Client', 'PrimaryPort'))):
                self.current_server = None
                return self.send_command(command)
            
            messagebox.showerror("Connection Error", f"Failed to communicate with server: {e}")
            return None
    
    def login(self):
        """Authenticate with server"""
        username = self.username_entry.get().strip()
        password = self.password_entry.get()
        
        if not username or not password:
            messagebox.showwarning("Input Required", "Please enter username and password")
            return
        
        # Connect to server
        if not self.connect_to_server():
            return
        
        # Send authentication request
        command = {
            'command': 'authenticate',
            'username': username,
            'password': password
        }
        
        response = self.send_command(command)
        
        if response and response.get('status') == 'success':
            self.authenticated = True
            self.session_token = response.get('session_token')
            self.username = username
            self.user_info = response.get('user_info')
            
            # Save username if remember is checked
            if self.remember_user_var.get():
                self.config.set('Client', 'LastUsername', username)
                self.save_config()
            
            # Update UI
            self.user_info_label.config(
                text=f"Logged in as: {self.user_info.get('name')} ({self.user_info.get('level')})",
                foreground='green'
            )
            self.notebook.tab(1, state='normal')
            self.notebook.select(1)
            self.login_button.config(text="Logout", command=self.logout)
            
            self.bottom_status.config(text=f"Logged in as {username}")
            
        else:
            message = response.get('message', 'Authentication failed') if response else 'Server error'
            messagebox.showerror("Login Failed", message)
            self.password_entry.delete(0, tk.END)
    
    def logout(self):
        """Logout"""
        self.authenticated = False
        self.session_token = None
        self.username = None
        self.user_info = None
        
        self.user_info_label.config(text="", foreground='black')
        self.password_entry.delete(0, tk.END)
        self.notebook.tab(1, state='disabled')
        self.notebook.select(0)
        self.login_button.config(text="Login", command=self.login)
        
        self.bottom_status.config(text="Logged out")
    
    def open_drawer(self):
        """Open cash drawer with current transaction"""
        if not self.authenticated:
            messagebox.showwarning("Not Authenticated", "Please login first")
            return
        
        # Get selected document types
        doc_types = [doc for doc, var in self.doc_vars.items() if var.get()]
        
        try:
            total = float(self.total_entry.get() or 0)
            amount_in = float(self.in_entry.get() or 0)
            amount_out = float(self.out_entry.get() or 0)
        except ValueError:
            messagebox.showerror("Invalid Input", "Please enter valid numbers")
            return
        
        command = {
            'command': 'open_drawer',
            'reason': 'Transaction',
            'document_type': ', '.join(doc_types) if doc_types else 'Manual',
            'total': total,
            'amount_in': amount_in,
            'amount_out': amount_out
        }
        
        response = self.send_command(command)
        
        if response and response.get('status') == 'success':
            self.bottom_status.config(text=f"Drawer opened - {datetime.now().strftime('%I:%M:%S %p')}")
            
            # Clear form
            for var in self.doc_vars.values():
                var.set(False)
            self.total_entry.delete(0, tk.END)
            self.total_entry.insert(0, "0.00")
            self.in_entry.delete(0, tk.END)
            self.in_entry.insert(0, "0.00")
            self.out_entry.delete(0, tk.END)
            self.out_entry.insert(0, "0.00")
        else:
            message = response.get('message', 'Failed to open drawer') if response else 'Server error'
            messagebox.showerror("Error", message)
    
    def quick_open(self, reason):
        """Quick open for BOD/EOD"""
        if not self.authenticated:
            messagebox.showwarning("Not Authenticated", "Please login first")
            return
        
        command = {
            'command': 'open_drawer',
            'reason': reason,
            'document_type': reason,
            'total': 0,
            'amount_in': 0,
            'amount_out': 0
        }
        
        response = self.send_command(command)
        
        if response and response.get('status') == 'success':
            self.bottom_status.config(text=f"{reason} - Drawer opened - {datetime.now().strftime('%I:%M:%S %p')}")
        else:
            message = response.get('message', 'Failed to open drawer') if response else 'Server error'
            messagebox.showerror("Error", message)
    
    def save_settings(self):
        """Save settings to config"""
        self.config.set('Client', 'PrimaryServer', self.primary_server_entry.get())
        self.config.set('Client', 'PrimaryPort', self.primary_port_entry.get())
        self.config.set('Client', 'SecondaryServer', self.secondary_server_entry.get())
        self.config.set('Client', 'SecondaryPort', self.secondary_port_entry.get())
        self.config.set('Client', 'AutoConnect', str(self.auto_connect_var.get()).lower())
        self.config.set('Client', 'RememberUsername', str(self.remember_user_var.get()).lower())
        
        self.save_config()
        messagebox.showinfo("Settings Saved", "Settings have been saved successfully")
        
        # Reconnect if server changed
        self.current_server = None
        if self.authenticated:
            self.connect_to_server()
    
    def discover_and_display_servers(self):
        """Discover servers and display results to user"""
        # Show progress
        original_text = self.bottom_status.cget('text')
        self.bottom_status.config(text="Searching for servers on network...")
        self.root.update()
        
        # Perform discovery
        discovered = self.scan_local_network()
        
        if not discovered:
            messagebox.showinfo(
                "No Servers Found",
                "Could not find any cash drawer servers on the network.\n\n"
                "Make sure:\n"
                "• Servers are running\n"
                "• You're on the same network\n"
                "• Firewall allows connections"
            )
            self.bottom_status.config(text=original_text)
            return
        
        # Display results
        result_text = f"Found {len(discovered)} server(s):\n\n"
        for i, (ip, port, server_id) in enumerate(discovered, 1):
            result_text += f"{i}. {server_id}\n   {ip}:{port}\n\n"
        
        result_text += "Would you like to use these servers?"
        
        use_servers = messagebox.askyesno("Servers Found", result_text, icon='question')
        
        if use_servers:
            # Set primary server
            ip1, port1, sid1 = discovered[0]
            self.primary_server_entry.delete(0, tk.END)
            self.primary_server_entry.insert(0, ip1)
            self.primary_port_entry.delete(0, tk.END)
            self.primary_port_entry.insert(0, str(port1))
            
            # Set secondary server if available
            if len(discovered) > 1:
                ip2, port2, sid2 = discovered[1]
                self.secondary_server_entry.delete(0, tk.END)
                self.secondary_server_entry.insert(0, ip2)
                self.secondary_port_entry.delete(0, tk.END)
                self.secondary_port_entry.insert(0, str(port2))
            
            self.bottom_status.config(text=f"Found {len(discovered)} server(s) - click Save Settings")
            messagebox.showinfo("Success", "Server addresses updated!\n\nClick 'Save Settings' to keep them.")
        else:
            self.bottom_status.config(text=original_text)
    
    def test_connection(self):
        """Test connection to servers"""
        results = []
        
        servers = [
            ("Primary", self.primary_server_entry.get(), self.primary_port_entry.get()),
            ("Secondary", self.secondary_server_entry.get(), self.secondary_port_entry.get())
        ]
        
        for name, host, port in servers:
            if not host:
                results.append(f"{name}: Not configured")
                continue
            
            try:
                sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
                sock.settimeout(3)
                sock.connect((host, int(port)))
                sock.close()
                results.append(f"{name} ({host}:{port}): ✓ Connected")
            except Exception as e:
                results.append(f"{name} ({host}:{port}): ✗ Failed - {str(e)}")
        
        messagebox.showinfo("Connection Test", "\n".join(results))
    
    def run(self):
        """Start the application"""
        self.root.mainloop()

if __name__ == '__main__':
    app = CashClient()
    app.run()
