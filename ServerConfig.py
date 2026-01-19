"""
Cash Server Configuration GUI
Easy-to-use interface for configuring the cash drawer server
"""

import tkinter as tk
from tkinter import ttk, messagebox, filedialog
import configparser
import os
import serial.tools.list_ports
import socket

class ServerConfigGUI:
    def __init__(self):
        self.root = tk.Tk()
        self.root.title("Cash Server Configuration")
        self.root.geometry("600x700")
        self.root.resizable(False, False)
        
        self.config = configparser.ConfigParser()
        self.config_file = 'server_config.ini'
        
        self.load_config()
        self.create_widgets()
    
    def load_config(self):
        """Load existing configuration or create default"""
        if os.path.exists(self.config_file):
            self.config.read(self.config_file)
        else:
            # Create default config
            self.config['Server'] = {
                'ServerID': 'SERVER1',
                'Port': '5000',
                'PeerServerHost': '',
                'PeerServerPort': '5000',
                'COMPort': 'COM10',
                'RelayPin': 'DTR',
                'RelayDuration': '0.5',
                'LogPath': r'\\partsrv2\Parts\Cash',
                'LocalLogPath': './logs',
                'EnableEmailAlerts': 'false',
                'SMTPServer': '',
                'AlertEmail': ''
            }
            
            self.config['Security'] = {
                'MaxFailedAttempts': '3',
                'LockoutDuration': '300',
                'SessionTimeout': '3600'
            }
    
    def save_config(self):
        """Save configuration to file"""
        with open(self.config_file, 'w') as f:
            self.config.write(f)
    
    def create_widgets(self):
        """Create GUI widgets"""
        # Create notebook for tabs
        notebook = ttk.Notebook(self.root)
        notebook.pack(fill=tk.BOTH, expand=True, padx=10, pady=10)
        
        # Server Settings Tab
        server_frame = ttk.Frame(notebook, padding=20)
        notebook.add(server_frame, text="Server Settings")
        self.create_server_tab(server_frame)
        
        # Hardware Tab
        hardware_frame = ttk.Frame(notebook, padding=20)
        notebook.add(hardware_frame, text="Hardware")
        self.create_hardware_tab(hardware_frame)
        
        # Logging Tab
        logging_frame = ttk.Frame(notebook, padding=20)
        notebook.add(logging_frame, text="Logging")
        self.create_logging_tab(logging_frame)
        
        # Security Tab
        security_frame = ttk.Frame(notebook, padding=20)
        notebook.add(security_frame, text="Security")
        self.create_security_tab(security_frame)
        
        # Bottom buttons
        button_frame = ttk.Frame(self.root)
        button_frame.pack(fill=tk.X, padx=10, pady=10)
        
        ttk.Button(
            button_frame,
            text="Save Configuration",
            command=self.save_configuration,
            width=20
        ).pack(side=tk.LEFT, padx=5)
        
        ttk.Button(
            button_frame,
            text="Test COM Port",
            command=self.test_com_port,
            width=20
        ).pack(side=tk.LEFT, padx=5)
        
        ttk.Button(
            button_frame,
            text="Exit",
            command=self.root.quit,
            width=15
        ).pack(side=tk.RIGHT, padx=5)
    
    def create_server_tab(self, parent):
        """Create server settings tab"""
        # Server Identity
        identity_frame = ttk.LabelFrame(parent, text="Server Identity", padding=15)
        identity_frame.pack(fill=tk.X, pady=10)
        
        ttk.Label(identity_frame, text="Server ID:").grid(row=0, column=0, sticky=tk.W, pady=5)
        self.server_id_entry = ttk.Entry(identity_frame, width=30)
        self.server_id_entry.grid(row=0, column=1, pady=5)
        self.server_id_entry.insert(0, self.config.get('Server', 'ServerID'))
        
        ttk.Label(identity_frame, text="Port:").grid(row=1, column=0, sticky=tk.W, pady=5)
        self.port_entry = ttk.Entry(identity_frame, width=30)
        self.port_entry.grid(row=1, column=1, pady=5)
        self.port_entry.insert(0, self.config.get('Server', 'Port'))
        
        # Get local IP
        try:
            s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
            s.connect(("8.8.8.8", 80))
            local_ip = s.getsockname()[0]
            s.close()
            ttk.Label(
                identity_frame,
                text=f"Local IP: {local_ip}",
                foreground='blue'
            ).grid(row=2, column=0, columnspan=2, pady=5)
        except:
            pass
        
        # Peer Server
        peer_frame = ttk.LabelFrame(parent, text="Peer Server (Optional)", padding=15)
        peer_frame.pack(fill=tk.X, pady=10)
        
        ttk.Label(peer_frame, text="Peer Host:").grid(row=0, column=0, sticky=tk.W, pady=5)
        self.peer_host_entry = ttk.Entry(peer_frame, width=30)
        self.peer_host_entry.grid(row=0, column=1, pady=5)
        self.peer_host_entry.insert(0, self.config.get('Server', 'PeerServerHost'))
        
        ttk.Label(peer_frame, text="Peer Port:").grid(row=1, column=0, sticky=tk.W, pady=5)
        self.peer_port_entry = ttk.Entry(peer_frame, width=30)
        self.peer_port_entry.grid(row=1, column=1, pady=5)
        self.peer_port_entry.insert(0, self.config.get('Server', 'PeerServerPort'))
        
        ttk.Label(
            peer_frame,
            text="Leave empty if running single server",
            font=('Arial', 8),
            foreground='gray'
        ).grid(row=2, column=0, columnspan=2)
    
    def create_hardware_tab(self, parent):
        """Create hardware settings tab"""
        # COM Port
        com_frame = ttk.LabelFrame(parent, text="Serial Port (COM Port)", padding=15)
        com_frame.pack(fill=tk.X, pady=10)
        
        ttk.Label(com_frame, text="COM Port:").grid(row=0, column=0, sticky=tk.W, pady=5)
        
        # COM port dropdown with available ports
        self.com_port_var = tk.StringVar()
        self.com_port_combo = ttk.Combobox(
            com_frame,
            textvariable=self.com_port_var,
            width=27,
            state='readonly'
        )
        self.com_port_combo.grid(row=0, column=1, pady=5)
        
        # Populate with available COM ports
        available_ports = [port.device for port in serial.tools.list_ports.comports()]
        self.com_port_combo['values'] = available_ports if available_ports else ['COM10']
        
        current_port = self.config.get('Server', 'COMPort')
        if current_port in available_ports:
            self.com_port_var.set(current_port)
        elif available_ports:
            self.com_port_var.set(available_ports[0])
        else:
            self.com_port_var.set('COM10')
        
        ttk.Button(
            com_frame,
            text="🔄 Refresh Ports",
            command=self.refresh_com_ports,
            width=15
        ).grid(row=0, column=2, padx=5)
        
        # Show available ports
        if available_ports:
            ports_text = "Available: " + ", ".join(available_ports)
        else:
            ports_text = "No COM ports detected"
        
        ttk.Label(
            com_frame,
            text=ports_text,
            font=('Arial', 8),
            foreground='blue'
        ).grid(row=1, column=0, columnspan=3, sticky=tk.W)
        
        # Relay Settings
        relay_frame = ttk.LabelFrame(parent, text="Relay Settings", padding=15)
        relay_frame.pack(fill=tk.X, pady=10)
        
        ttk.Label(relay_frame, text="Relay Pin:").grid(row=0, column=0, sticky=tk.W, pady=5)
        self.relay_pin_var = tk.StringVar(value=self.config.get('Server', 'RelayPin'))
        relay_pin_combo = ttk.Combobox(
            relay_frame,
            textvariable=self.relay_pin_var,
            values=['DTR', 'RTS'],
            width=27,
            state='readonly'
        )
        relay_pin_combo.grid(row=0, column=1, pady=5)
        
        ttk.Label(relay_frame, text="Duration (seconds):").grid(row=1, column=0, sticky=tk.W, pady=5)
        self.relay_duration_entry = ttk.Entry(relay_frame, width=30)
        self.relay_duration_entry.grid(row=1, column=1, pady=5)
        self.relay_duration_entry.insert(0, self.config.get('Server', 'RelayDuration', fallback='0.5'))
        
        ttk.Label(
            relay_frame,
            text="How long to keep relay energized (default: 0.5 seconds)",
            font=('Arial', 8),
            foreground='gray'
        ).grid(row=2, column=0, columnspan=2, sticky=tk.W)
        
        # Test button
        ttk.Button(
            parent,
            text="🔧 Test Relay (Open Drawer)",
            command=self.test_relay,
            width=30
        ).pack(pady=10)
    
    def create_logging_tab(self, parent):
        """Create logging settings tab"""
        # Network Path
        network_frame = ttk.LabelFrame(parent, text="Network Logging", padding=15)
        network_frame.pack(fill=tk.X, pady=10)
        
        ttk.Label(network_frame, text="Network Path:").grid(row=0, column=0, sticky=tk.W, pady=5)
        self.log_path_entry = ttk.Entry(network_frame, width=40)
        self.log_path_entry.grid(row=0, column=1, pady=5)
        self.log_path_entry.insert(0, self.config.get('Server', 'LogPath'))
        
        ttk.Button(
            network_frame,
            text="Browse...",
            command=self.browse_log_path,
            width=10
        ).grid(row=0, column=2, padx=5)
        
        ttk.Label(
            network_frame,
            text="UNC path like: \\\\server\\share\\folder",
            font=('Arial', 8),
            foreground='gray'
        ).grid(row=1, column=0, columnspan=3, sticky=tk.W)
        
        # Local Path
        local_frame = ttk.LabelFrame(parent, text="Local Logging (Fallback)", padding=15)
        local_frame.pack(fill=tk.X, pady=10)
        
        ttk.Label(local_frame, text="Local Path:").grid(row=0, column=0, sticky=tk.W, pady=5)
        self.local_log_path_entry = ttk.Entry(local_frame, width=40)
        self.local_log_path_entry.grid(row=0, column=1, pady=5)
        self.local_log_path_entry.insert(0, self.config.get('Server', 'LocalLogPath'))
        
        ttk.Button(
            local_frame,
            text="Browse...",
            command=self.browse_local_log_path,
            width=10
        ).grid(row=0, column=2, padx=5)
        
        # Email Alerts
        email_frame = ttk.LabelFrame(parent, text="Email Alerts (Optional)", padding=15)
        email_frame.pack(fill=tk.X, pady=10)
        
        self.email_enabled_var = tk.BooleanVar(
            value=self.config.get('Server', 'EnableEmailAlerts', fallback='false').lower() == 'true'
        )
        ttk.Checkbutton(
            email_frame,
            text="Enable email alerts for security events",
            variable=self.email_enabled_var
        ).grid(row=0, column=0, columnspan=2, sticky=tk.W, pady=5)
        
        ttk.Label(email_frame, text="SMTP Server:").grid(row=1, column=0, sticky=tk.W, pady=5)
        self.smtp_entry = ttk.Entry(email_frame, width=40)
        self.smtp_entry.grid(row=1, column=1, pady=5)
        self.smtp_entry.insert(0, self.config.get('Server', 'SMTPServer'))
        
        ttk.Label(email_frame, text="Alert Email:").grid(row=2, column=0, sticky=tk.W, pady=5)
        self.alert_email_entry = ttk.Entry(email_frame, width=40)
        self.alert_email_entry.grid(row=2, column=1, pady=5)
        self.alert_email_entry.insert(0, self.config.get('Server', 'AlertEmail'))
    
    def create_security_tab(self, parent):
        """Create security settings tab"""
        security_frame = ttk.LabelFrame(parent, text="Account Security", padding=15)
        security_frame.pack(fill=tk.X, pady=10)
        
        ttk.Label(security_frame, text="Max Failed Attempts:").grid(row=0, column=0, sticky=tk.W, pady=5)
        self.max_attempts_entry = ttk.Entry(security_frame, width=30)
        self.max_attempts_entry.grid(row=0, column=1, pady=5)
        self.max_attempts_entry.insert(0, self.config.get('Security', 'MaxFailedAttempts'))
        
        ttk.Label(security_frame, text="Lockout Duration (seconds):").grid(row=1, column=0, sticky=tk.W, pady=5)
        self.lockout_duration_entry = ttk.Entry(security_frame, width=30)
        self.lockout_duration_entry.grid(row=1, column=1, pady=5)
        self.lockout_duration_entry.insert(0, self.config.get('Security', 'LockoutDuration'))
        
        ttk.Label(security_frame, text="Session Timeout (seconds):").grid(row=2, column=0, sticky=tk.W, pady=5)
        self.session_timeout_entry = ttk.Entry(security_frame, width=30)
        self.session_timeout_entry.grid(row=2, column=1, pady=5)
        self.session_timeout_entry.insert(0, self.config.get('Security', 'SessionTimeout'))
        
        # Info labels
        info_frame = ttk.Frame(parent, padding=15)
        info_frame.pack(fill=tk.X, pady=10)
        
        ttk.Label(
            info_frame,
            text="Recommended Settings:",
            font=('Arial', 10, 'bold')
        ).pack(anchor=tk.W)
        
        ttk.Label(
            info_frame,
            text="• Max Failed Attempts: 3-5 attempts",
            font=('Arial', 9)
        ).pack(anchor=tk.W, padx=20)
        
        ttk.Label(
            info_frame,
            text="• Lockout Duration: 300 seconds (5 minutes)",
            font=('Arial', 9)
        ).pack(anchor=tk.W, padx=20)
        
        ttk.Label(
            info_frame,
            text="• Session Timeout: 3600 seconds (1 hour)",
            font=('Arial', 9)
        ).pack(anchor=tk.W, padx=20)
    
    def refresh_com_ports(self):
        """Refresh available COM ports"""
        available_ports = [port.device for port in serial.tools.list_ports.comports()]
        self.com_port_combo['values'] = available_ports if available_ports else ['COM10']
        
        if available_ports:
            messagebox.showinfo(
                "COM Ports",
                f"Found {len(available_ports)} port(s):\n" + "\n".join(available_ports)
            )
        else:
            messagebox.showwarning("COM Ports", "No COM ports detected")
    
    def browse_log_path(self):
        """Browse for network log path"""
        path = filedialog.askdirectory(title="Select Network Log Directory")
        if path:
            self.log_path_entry.delete(0, tk.END)
            self.log_path_entry.insert(0, path)
    
    def browse_local_log_path(self):
        """Browse for local log path"""
        path = filedialog.askdirectory(title="Select Local Log Directory")
        if path:
            self.local_log_path_entry.delete(0, tk.END)
            self.local_log_path_entry.insert(0, path)
    
    def save_configuration(self):
        """Save all configuration settings"""
        try:
            # Server settings
            self.config.set('Server', 'ServerID', self.server_id_entry.get())
            self.config.set('Server', 'Port', self.port_entry.get())
            self.config.set('Server', 'PeerServerHost', self.peer_host_entry.get())
            self.config.set('Server', 'PeerServerPort', self.peer_port_entry.get())
            
            # Hardware settings
            self.config.set('Server', 'COMPort', self.com_port_var.get())
            self.config.set('Server', 'RelayPin', self.relay_pin_var.get())
            self.config.set('Server', 'RelayDuration', self.relay_duration_entry.get())
            
            # Logging settings
            self.config.set('Server', 'LogPath', self.log_path_entry.get())
            self.config.set('Server', 'LocalLogPath', self.local_log_path_entry.get())
            self.config.set('Server', 'EnableEmailAlerts', str(self.email_enabled_var.get()).lower())
            self.config.set('Server', 'SMTPServer', self.smtp_entry.get())
            self.config.set('Server', 'AlertEmail', self.alert_email_entry.get())
            
            # Security settings
            self.config.set('Security', 'MaxFailedAttempts', self.max_attempts_entry.get())
            self.config.set('Security', 'LockoutDuration', self.lockout_duration_entry.get())
            self.config.set('Security', 'SessionTimeout', self.session_timeout_entry.get())
            
            # Save to file
            self.save_config()
            
            messagebox.showinfo(
                "Success",
                f"Configuration saved to {self.config_file}\n\n"
                "Restart the server for changes to take effect."
            )
            
        except Exception as e:
            messagebox.showerror("Error", f"Failed to save configuration:\n{e}")
    
    def test_com_port(self):
        """Test COM port connectivity"""
        com_port = self.com_port_var.get()
        
        try:
            import serial
            ser = serial.Serial(port=com_port, baudrate=9600, timeout=1)
            ser.close()
            
            messagebox.showinfo(
                "COM Port Test",
                f"✓ Successfully connected to {com_port}\n\n"
                "The port is available and ready to use."
            )
        except Exception as e:
            messagebox.showerror(
                "COM Port Test Failed",
                f"✗ Could not connect to {com_port}\n\n"
                f"Error: {e}\n\n"
                "Check:\n"
                "• Port is not in use by another program\n"
                "• USB adapter is connected\n"
                "• Drivers are installed\n"
                "• Port number is correct"
            )
    
    def test_relay(self):
        """Test relay by triggering it"""
        if not messagebox.askyesno(
            "Test Relay",
            "This will attempt to open the cash drawer.\n\n"
            "Make sure the relay and drawer are connected.\n\n"
            "Continue?"
        ):
            return
        
        com_port = self.com_port_var.get()
        relay_pin = self.relay_pin_var.get()
        
        try:
            duration = float(self.relay_duration_entry.get())
        except:
            messagebox.showerror("Error", "Invalid relay duration. Must be a number.")
            return
        
        try:
            import serial
            import time
            
            ser = serial.Serial(port=com_port, baudrate=9600, timeout=1)
            
            if relay_pin == 'DTR':
                ser.dtr = True
                time.sleep(duration)
                ser.dtr = False
            else:  # RTS
                ser.rts = True
                time.sleep(duration)
                ser.rts = False
            
            ser.close()
            
            messagebox.showinfo(
                "Test Complete",
                f"✓ Relay triggered successfully!\n\n"
                f"Pin: {relay_pin}\n"
                f"Duration: {duration} seconds\n\n"
                "Did the drawer open?"
            )
            
        except Exception as e:
            messagebox.showerror(
                "Test Failed",
                f"✗ Could not trigger relay\n\n"
                f"Error: {e}\n\n"
                "Check:\n"
                "• COM port is correct\n"
                "• Relay is connected\n"
                "• Hardware is powered\n"
                "• Wiring is correct"
            )
    
    def run(self):
        """Start the GUI"""
        self.root.mainloop()

if __name__ == '__main__':
    app = ServerConfigGUI()
    app.run()
