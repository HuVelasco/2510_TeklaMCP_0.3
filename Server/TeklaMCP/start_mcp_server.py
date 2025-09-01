#!/usr/bin/env python3
"""
TeklaMCP Production Server Startup Script
Automatically starts both HTTP backend and MCP server
Single command startup: python start_mcp_server.py
"""

import os
import sys
import json
import time
import subprocess
import signal
import atexit
from pathlib import Path

# Global variable to track HTTP backend process
http_backend_process = None

def load_config():
    """Load MCP configuration"""
    config_path = Path(__file__).parent / "config" / "mcp_config.json"
    if config_path.exists():
        with open(config_path) as f:
            return json.load(f)
    return {}

def start_http_backend():
    """Start the C# HTTP backend server"""
    global http_backend_process
    
    print("=" * 60)
    print("Starting TeklaMCP HTTP Backend (C#)...")
    print("=" * 60)
    
    try:
        # Start HTTP backend in the background
        server_dir = Path(__file__).parent
        http_backend_process = subprocess.Popen(
            ["dotnet", "run", "--project", "TeklaMCPSimple.csproj"],
            cwd=server_dir,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
            bufsize=1,
            universal_newlines=True
        )
        
        # Wait for server to start and check for successful startup
        print("Waiting for HTTP backend to initialize...")
        start_time = time.time()
        timeout = 10  # 10 seconds timeout
        
        while time.time() - start_time < timeout:
            if http_backend_process.poll() is not None:
                # Process ended unexpectedly
                print("ERROR: HTTP backend failed to start")
                stderr_output = http_backend_process.stderr.read()
                if stderr_output:
                    print(f"Error details: {stderr_output}")
                return False
            
            # Check if we can read from stdout (non-blocking check)
            line = http_backend_process.stdout.readline()
            if line:
                print(f"  HTTP Backend: {line.strip()}")
                if "Server listening on port" in line:
                    print("[OK] HTTP backend started successfully!")
                    return True
            
            time.sleep(0.5)
        
        # If we get here, assume it started (timeout reached but process still running)
        if http_backend_process.poll() is None:
            print("[OK] HTTP backend appears to be running")
            return True
        else:
            print("ERROR: HTTP backend failed to start within timeout")
            return False
            
    except FileNotFoundError:
        print("ERROR: dotnet command not found. Please install .NET SDK")
        return False
    except Exception as e:
        print(f"ERROR starting HTTP backend: {e}")
        return False

def cleanup_processes():
    """Clean up background processes on exit"""
    global http_backend_process
    
    if http_backend_process and http_backend_process.poll() is None:
        print("\nShutting down HTTP backend...")
        http_backend_process.terminate()
        try:
            http_backend_process.wait(timeout=5)
        except subprocess.TimeoutExpired:
            http_backend_process.kill()
        print("HTTP backend stopped")

def signal_handler(sig, frame):
    """Handle Ctrl+C gracefully"""
    print("\n\nShutdown signal received...")
    cleanup_processes()
    sys.exit(0)

def main():
    """Start both HTTP backend and MCP server"""
    print("=" * 60)
    print("  TeklaMCP AUTOSTART LAUNCHER")
    print("  Phase 1 Production System")
    print("=" * 60)
    
    # Load configuration
    config = load_config()
    server_info = config.get("server", {})
    backend_config = config.get("tekla_backend", {})
    
    print(f"Server: {server_info.get('name', 'TeklaMCP Server')}")
    print(f"Version: {server_info.get('version', '1.0.0')}")
    print(f"Backend URL: {backend_config.get('url', 'http://localhost:8081')}")
    print()
    
    # Register cleanup handlers
    atexit.register(cleanup_processes)
    signal.signal(signal.SIGINT, signal_handler)
    signal.signal(signal.SIGTERM, signal_handler)
    
    # Step 1: Start HTTP backend
    if not start_http_backend():
        print("Failed to start HTTP backend. Exiting...")
        sys.exit(1)
    
    # Step 2: Give it a moment to fully initialize
    print("\nWaiting for HTTP backend to stabilize...")
    time.sleep(2)
    
    # Step 3: Start MCP server
    print("\n" + "=" * 60)
    print("Starting FastMCP Server...")
    print("=" * 60)
    
    try:
        from mcp_server_simplified import mcp
        print("FastMCP server starting...")
        print("System ready! Both servers running.")
        print("\nPress Ctrl+C to shutdown both servers")
        print("=" * 60 + "\n")
        
        # This will block and run the MCP server
        mcp.run()
        
    except ImportError as e:
        print(f"ERROR importing MCP server: {e}")
        print("Make sure requirements are installed: pip install -r requirements.txt")
        cleanup_processes()
        sys.exit(1)
    except KeyboardInterrupt:
        print("\nShutdown requested...")
    except Exception as e:
        print(f"ERROR starting MCP server: {e}")
        cleanup_processes()
        sys.exit(1)
    finally:
        cleanup_processes()

if __name__ == "__main__":
    main()