#!/usr/bin/env python3
"""
TeklaMCP Production Server Startup Script
Loads configuration and starts the MCP server
"""

import os
import sys
import json
from pathlib import Path

def load_config():
    """Load MCP configuration"""
    config_path = Path(__file__).parent / "config" / "mcp_config.json"
    if config_path.exists():
        with open(config_path) as f:
            return json.load(f)
    return {}

def main():
    """Start the production MCP server"""
    print("Starting TeklaMCP Production Server...")
    
    # Load configuration
    config = load_config()
    server_info = config.get("server", {})
    
    print(f"Server: {server_info.get('name', 'TeklaMCP Server')}")
    print(f"Version: {server_info.get('version', '1.0.0')}")
    print(f"Backend URL: {config.get('tekla_backend', {}).get('url', 'http://localhost:8081')}")
    
    # Import and start the MCP server
    try:
        from mcp_server_simplified import mcp
        print("Production MCP server starting...")
        mcp.run()
    except ImportError as e:
        print(f"Error importing MCP server: {e}")
        print("Make sure requirements are installed: pip install -r requirements.txt")
        sys.exit(1)
    except Exception as e:
        print(f"Error starting server: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()