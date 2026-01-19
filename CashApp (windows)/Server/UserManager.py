"""
User Management Utility for Cash Server
Manage users, passwords, and permissions
"""

import json
import hashlib
import getpass
import os
from datetime import datetime

class UserManager:
    def __init__(self, user_file='users.json'):
        self.user_file = user_file
        self.users = {}
        self.load_users()
    
    def load_users(self):
        """Load users from file"""
        if os.path.exists(self.user_file):
            with open(self.user_file, 'r') as f:
                self.users = json.load(f)
        else:
            print(f"User file not found. Creating new one.")
            self.users = {}
    
    def save_users(self):
        """Save users to file"""
        with open(self.user_file, 'w') as f:
            json.dump(self.users, f, indent=2)
        print(f"Users saved to {self.user_file}")
    
    def hash_password(self, password):
        """Hash password"""
        return hashlib.sha256(password.encode()).hexdigest()
    
    def list_users(self):
        """List all users"""
        if not self.users:
            print("No users found.")
            return
        
        print("\n" + "="*60)
        print(f"{'Username':<15} {'Name':<20} {'Level':<10} {'Status':<10}")
        print("="*60)
        
        for username, info in self.users.items():
            status = "LOCKED" if info.get('locked_until') else "Active"
            print(f"{username:<15} {info.get('name', 'N/A'):<20} {info.get('level', 'user'):<10} {status:<10}")
        
        print("="*60 + "\n")
    
    def add_user(self):
        """Add a new user"""
        print("\n--- Add New User ---")
        
        username = input("Username: ").strip().lower()
        
        if username in self.users:
            print(f"Error: User '{username}' already exists!")
            return
        
        name = input("Full Name: ").strip()
        
        level = input("Level (user/admin) [user]: ").strip().lower()
        if level not in ['user', 'admin']:
            level = 'user'
        
        password = getpass.getpass("Password: ")
        password_confirm = getpass.getpass("Confirm Password: ")
        
        if password != password_confirm:
            print("Error: Passwords don't match!")
            return
        
        if len(password) < 6:
            print("Error: Password must be at least 6 characters!")
            return
        
        self.users[username] = {
            'password_hash': self.hash_password(password),
            'name': name,
            'level': level,
            'failed_attempts': 0,
            'locked_until': None,
            'created': datetime.now().isoformat()
        }
        
        self.save_users()
        print(f"\nUser '{username}' created successfully!")
    
    def change_password(self):
        """Change user password"""
        print("\n--- Change Password ---")
        
        username = input("Username: ").strip().lower()
        
        if username not in self.users:
            print(f"Error: User '{username}' not found!")
            return
        
        password = getpass.getpass("New Password: ")
        password_confirm = getpass.getpass("Confirm Password: ")
        
        if password != password_confirm:
            print("Error: Passwords don't match!")
            return
        
        if len(password) < 6:
            print("Error: Password must be at least 6 characters!")
            return
        
        self.users[username]['password_hash'] = self.hash_password(password)
        self.users[username]['failed_attempts'] = 0
        self.users[username]['locked_until'] = None
        
        self.save_users()
        print(f"\nPassword for '{username}' changed successfully!")
    
    def delete_user(self):
        """Delete a user"""
        print("\n--- Delete User ---")
        
        username = input("Username: ").strip().lower()
        
        if username not in self.users:
            print(f"Error: User '{username}' not found!")
            return
        
        confirm = input(f"Are you sure you want to delete '{username}'? (yes/no): ").strip().lower()
        
        if confirm == 'yes':
            del self.users[username]
            self.save_users()
            print(f"\nUser '{username}' deleted successfully!")
        else:
            print("Delete cancelled.")
    
    def unlock_user(self):
        """Unlock a locked user account"""
        print("\n--- Unlock User ---")
        
        username = input("Username: ").strip().lower()
        
        if username not in self.users:
            print(f"Error: User '{username}' not found!")
            return
        
        self.users[username]['failed_attempts'] = 0
        self.users[username]['locked_until'] = None
        
        self.save_users()
        print(f"\nUser '{username}' unlocked successfully!")
    
    def show_menu(self):
        """Show main menu"""
        while True:
            print("\n" + "="*40)
            print("Cash Server - User Management")
            print("="*40)
            print("1. List Users")
            print("2. Add User")
            print("3. Change Password")
            print("4. Delete User")
            print("5. Unlock User")
            print("6. Exit")
            print("="*40)
            
            choice = input("\nSelect option: ").strip()
            
            if choice == '1':
                self.list_users()
            elif choice == '2':
                self.add_user()
            elif choice == '3':
                self.change_password()
            elif choice == '4':
                self.delete_user()
            elif choice == '5':
                self.unlock_user()
            elif choice == '6':
                print("\nGoodbye!")
                break
            else:
                print("Invalid option!")

if __name__ == '__main__':
    manager = UserManager()
    manager.show_menu()
