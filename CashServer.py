"""
Cash Drawer Server Application
Handles hardware control (COM port relay) and client authentication
"""

import socket
import threading
import json
import logging
import serial
import time
import os
from datetime import datetime
from pathlib import Path
import hashlib
import secrets
import configparser

class CashServer:
    def __init__(self, config_file='server_config.ini'):
        self.config = configparser.ConfigParser()
        self.config_file = config_file
        self.load_config()
        
        # Setup logging
        self.setup_logging()
        
        # Server state
        self.running = False
        self.clients = []
        self.serial_port = None
        self.peer_server = None
        
        # Authentication
        self.users = {}
        self.load_users()
        
        # Server discovery
        self.server_id = self.config.get('Server', 'ServerID', fallback='SERVER1')
        self.peer_status = {'online': False, 'last_seen': None}
        
    def load_config(self):
        """Load or create configuration file"""
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
                'EnableEmailAlerts': 'true',
                'SMTPServer': '',
                'AlertEmail': ''
            }
            
            self.config['Security'] = {
                'MaxFailedAttempts': '3',
                'LockoutDuration': '300',
                'SessionTimeout': '3600'
            }
            
            with open(self.config_file, 'w') as f:
                self.config.write(f)
            
            print(f"Created default config file: {self.config_file}")
            print("Please edit the configuration and restart the server.")
    
    def setup_logging(self):
        """Setup logging to both network share and local"""
        log_path = self.config.get('Server', 'LogPath', fallback='./logs')
        local_log_path = self.config.get('Server', 'LocalLogPath', fallback='./logs')
        
        # Create local log directory
        Path(local_log_path).mkdir(parents=True, exist_ok=True)
        
        # Try to create network log directory
        try:
            Path(log_path).mkdir(parents=True, exist_ok=True)
            network_available = True
        except:
            network_available = False
            log_path = local_log_path
        
        # Setup logging
        log_file = os.path.join(log_path, f'CashServer_{self.server_id}_{datetime.now().strftime("%Y%m%d")}.log')
        
        logging.basicConfig(
            level=logging.INFO,
            format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
            handlers=[
                logging.FileHandler(log_file),
                logging.StreamHandler()
            ]
        )
        
        self.logger = logging.getLogger('CashServer')
        
        if not network_available:
            self.logger.warning(f"Network log path not available, using local: {local_log_path}")
    
    def load_users(self):
        """Load user credentials from file"""
        user_file = 'users.json'
        if os.path.exists(user_file):
            with open(user_file, 'r') as f:
                self.users = json.load(f)
        else:
            # Create default admin user
            self.users = {
                'admin': {
                    'password_hash': self.hash_password('admin123'),
                    'name': 'Administrator',
                    'level': 'admin',
                    'failed_attempts': 0,
                    'locked_until': None
                }
            }
            self.save_users()
            print("Created default admin user (username: admin, password: admin123)")
            print("PLEASE CHANGE THIS PASSWORD IMMEDIATELY!")
    
    def save_users(self):
        """Save user credentials to file"""
        with open('users.json', 'w') as f:
            json.dump(self.users, f, indent=2)
    
    def hash_password(self, password):
        """Hash password using SHA-256"""
        return hashlib.sha256(password.encode()).hexdigest()
    
    def authenticate_user(self, username, password):
        """Authenticate user credentials"""
        if username not in self.users:
            self.logger.warning(f"Authentication failed: Unknown user '{username}'")
            return False, "Invalid username or password"
        
        user = self.users[username]
        
        # Check if account is locked
        if user.get('locked_until'):
            lock_time = datetime.fromisoformat(user['locked_until'])
            if datetime.now() < lock_time:
                self.logger.warning(f"Authentication failed: Account '{username}' is locked")
                return False, "Account is locked. Please try again later."
            else:
                user['locked_until'] = None
                user['failed_attempts'] = 0
        
        # Verify password
        if user['password_hash'] == self.hash_password(password):
            user['failed_attempts'] = 0
            self.save_users()
            self.logger.info(f"User '{username}' authenticated successfully")
            return True, {"name": user['name'], "level": user['level']}
        else:
            # Increment failed attempts
            user['failed_attempts'] = user.get('failed_attempts', 0) + 1
            max_attempts = int(self.config.get('Security', 'MaxFailedAttempts', fallback='3'))
            
            if user['failed_attempts'] >= max_attempts:
                lockout_duration = int(self.config.get('Security', 'LockoutDuration', fallback='300'))
                user['locked_until'] = (datetime.now() + 
                                       timedelta(seconds=lockout_duration)).isoformat()
                self.logger.warning(f"Account '{username}' locked due to too many failed attempts")
                self.send_security_alert(username, "Account locked")
            
            self.save_users()
            self.logger.warning(f"Authentication failed for user '{username}'")
            return False, "Invalid username or password"
    
    def send_security_alert(self, username, reason):
        """Send email alert for security events"""
        if self.config.get('Server', 'EnableEmailAlerts', fallback='true').lower() == 'true':
            # TODO: Implement email sending
            self.logger.warning(f"SECURITY ALERT: {reason} for user '{username}'")
    
    def initialize_serial(self):
        """Initialize serial port connection"""
        com_port = self.config.get('Server', 'COMPort', fallback='COM10')
        
        try:
            self.serial_port = serial.Serial(
                port=com_port,
                baudrate=9600,
                timeout=1
            )
            self.logger.info(f"Serial port {com_port} initialized successfully")
            return True
        except Exception as e:
            self.logger.error(f"Failed to initialize serial port {com_port}: {e}")
            return False
    
    def open_drawer(self, reason, user, document_type="", total=0, amount_in=0, amount_out=0):
        """Open cash drawer and log the transaction"""
        if not self.serial_port:
            if not self.initialize_serial():
                return False, "Serial port not available"
        
        try:
            # Trigger relay
            relay_pin = self.config.get('Server', 'RelayPin', fallback='DTR')
            relay_duration = float(self.config.get('Server', 'RelayDuration', fallback='0.5'))
            
            if relay_pin == 'DTR':
                self.serial_port.dtr = True
                time.sleep(relay_duration)
                self.serial_port.dtr = False
            elif relay_pin == 'RTS':
                self.serial_port.rts = True
                time.sleep(relay_duration)
                self.serial_port.rts = False
            
            # Log transaction
            log_entry = {
                'timestamp': datetime.now().isoformat(),
                'server': self.server_id,
                'user': user,
                'reason': reason,
                'document_type': document_type,
                'total': total,
                'in': amount_in,
                'out': amount_out
            }
            
            self.log_transaction(log_entry)
            self.logger.info(f"Drawer opened by {user} - Reason: {reason}")
            
            return True, "Drawer opened successfully"
            
        except Exception as e:
            self.logger.error(f"Failed to open drawer: {e}")
            return False, f"Error: {str(e)}"
    
    def log_transaction(self, entry):
        """Log transaction to file"""
        log_path = self.config.get('Server', 'LogPath', fallback='./logs')
        local_log_path = self.config.get('Server', 'LocalLogPath', fallback='./logs')
        
        log_line = (f"{entry['timestamp']} | {entry['server']} | {entry['user']} | "
                   f"{entry['reason']} | {entry['document_type']} | "
                   f"Total: {entry['total']} | IN: {entry['in']} | OUT: {entry['out']}\n")
        
        # Try network path first, fall back to local
        for path in [log_path, local_log_path]:
            try:
                Path(path).mkdir(parents=True, exist_ok=True)
                trans_file = os.path.join(path, f'Transactions_{datetime.now().strftime("%Y%m")}.log')
                with open(trans_file, 'a') as f:
                    f.write(log_line)
                break
            except Exception as e:
                self.logger.warning(f"Failed to write to {path}: {e}")
    
    def handle_client(self, client_socket, address):
        """Handle client connection"""
        self.logger.info(f"Client connected from {address}")
        authenticated = False
        username = None
        
        try:
            while True:
                data = client_socket.recv(4096)
                if not data:
                    break
                
                try:
                    request = json.loads(data.decode())
                    command = request.get('command')
                    
                    if command == 'authenticate':
                        success, result = self.authenticate_user(
                            request.get('username'),
                            request.get('password')
                        )
                        
                        if success:
                            authenticated = True
                            username = request.get('username')
                            session_token = secrets.token_hex(16)
                            response = {
                                'status': 'success',
                                'session_token': session_token,
                                'user_info': result,
                                'server_id': self.server_id
                            }
                        else:
                            response = {'status': 'error', 'message': result}
                    
                    elif not authenticated:
                        response = {'status': 'error', 'message': 'Not authenticated'}
                    
                    elif command == 'open_drawer':
                        success, message = self.open_drawer(
                            reason=request.get('reason', 'Manual'),
                            user=username,
                            document_type=request.get('document_type', ''),
                            total=request.get('total', 0),
                            amount_in=request.get('amount_in', 0),
                            amount_out=request.get('amount_out', 0)
                        )
                        response = {
                            'status': 'success' if success else 'error',
                            'message': message
                        }
                    
                    elif command == 'get_status':
                        response = {
                            'status': 'success',
                            'server_id': self.server_id,
                            'serial_port': self.config.get('Server', 'COMPort'),
                            'peer_status': self.peer_status
                        }
                    
                    elif command == 'ping':
                        response = {'status': 'success', 'server_id': self.server_id}
                    
                    else:
                        response = {'status': 'error', 'message': 'Unknown command'}
                    
                    client_socket.send(json.dumps(response).encode())
                    
                except json.JSONDecodeError:
                    self.logger.error("Invalid JSON received from client")
                    
        except Exception as e:
            self.logger.error(f"Error handling client {address}: {e}")
        finally:
            client_socket.close()
            self.logger.info(f"Client {address} disconnected")
    
    def peer_heartbeat(self):
        """Send heartbeat to peer server"""
        peer_host = self.config.get('Server', 'PeerServerHost', fallback='')
        if not peer_host:
            return
        
        peer_port = int(self.config.get('Server', 'PeerServerPort', fallback='5000'))
        
        while self.running:
            try:
                sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
                sock.settimeout(5)
                sock.connect((peer_host, peer_port))
                
                request = {'command': 'ping'}
                sock.send(json.dumps(request).encode())
                
                response = json.loads(sock.recv(1024).decode())
                
                if response.get('status') == 'success':
                    self.peer_status = {
                        'online': True,
                        'last_seen': datetime.now().isoformat(),
                        'server_id': response.get('server_id')
                    }
                
                sock.close()
                
            except Exception as e:
                self.peer_status = {'online': False, 'last_seen': None}
            
            time.sleep(30)  # Heartbeat every 30 seconds
    
    def discovery_listener(self):
        """Listen for UDP broadcast discovery requests"""
        try:
            # Create UDP socket for discovery
            sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
            sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
            sock.bind(('', 5001))  # Listen on discovery port
            sock.settimeout(1)
            
            self.logger.info("Discovery listener started on UDP port 5001")
            
            while self.running:
                try:
                    data, addr = sock.recvfrom(1024)
                    request = json.loads(data.decode())
                    
                    if request.get('command') == 'discover' and request.get('type') == 'cash_client':
                        # Respond with server information
                        response = {
                            'type': 'cash_server',
                            'server_id': self.server_id,
                            'port': int(self.config.get('Server', 'Port', fallback='5000')),
                            'version': '3.0'
                        }
                        
                        sock.sendto(json.dumps(response).encode(), addr)
                        self.logger.info(f"Responded to discovery request from {addr[0]}")
                        
                except socket.timeout:
                    continue
                except Exception as e:
                    if self.running:
                        self.logger.error(f"Discovery listener error: {e}")
            
            sock.close()
            self.logger.info("Discovery listener stopped")
            
        except Exception as e:
            self.logger.error(f"Failed to start discovery listener: {e}")
    
    def start(self):
        """Start the server"""
        self.running = True
        
        # Initialize serial port
        self.initialize_serial()
        
        # Start peer heartbeat thread
        if self.config.get('Server', 'PeerServerHost', fallback=''):
            peer_thread = threading.Thread(target=self.peer_heartbeat, daemon=True)
            peer_thread.start()
        
        # Start discovery listener thread
        discovery_thread = threading.Thread(target=self.discovery_listener, daemon=True)
        discovery_thread.start()
        
        # Start server socket
        server_port = int(self.config.get('Server', 'Port', fallback='5000'))
        server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        server_socket.bind(('0.0.0.0', server_port))
        server_socket.listen(5)
        
        self.logger.info(f"Cash Server {self.server_id} started on port {server_port}")
        print(f"Server {self.server_id} is running on port {server_port}")
        print("Press Ctrl+C to stop")
        
        try:
            while self.running:
                client_socket, address = server_socket.accept()
                client_thread = threading.Thread(
                    target=self.handle_client,
                    args=(client_socket, address),
                    daemon=True
                )
                client_thread.start()
        except KeyboardInterrupt:
            print("\nShutting down server...")
        finally:
            self.running = False
            if self.serial_port:
                self.serial_port.close()
            server_socket.close()
            self.logger.info("Server stopped")

if __name__ == '__main__':
    from datetime import timedelta
    server = CashServer()
    server.start()
