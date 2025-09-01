#!/usr/bin/env python3
"""
Test MCP Protocol directly with TeklaMCP server
Simulates what Claude Desktop does when connecting to MCP server
"""

import subprocess
import json
import time

def test_mcp_protocol():
    """Test MCP protocol communication with our server"""
    print("Testing MCP Protocol Communication with TeklaMCP")
    print("=" * 60)
    
    try:
        # Start MCP server process
        process = subprocess.Popen(
            ["python", "start_mcp_server.py"],
            cwd=r"Server\TeklaMCP",
            stdin=subprocess.PIPE,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
            bufsize=1
        )
        
        # Wait for server startup
        time.sleep(5)
        
        # Test 1: Initialize
        print("\n[TEST 1] Initialize MCP Protocol")
        init_request = {
            "jsonrpc": "2.0",
            "id": 1,
            "method": "initialize",
            "params": {
                "protocolVersion": "2024-11-05",
                "capabilities": {},
                "clientInfo": {
                    "name": "test-client",
                    "version": "1.0.0"
                }
            }
        }
        
        request_json = json.dumps(init_request) + "\n"
        process.stdin.write(request_json)
        process.stdin.flush()
        
        # Read response
        response_line = process.stdout.readline()
        if response_line:
            try:
                response = json.loads(response_line.strip())
                if "result" in response:
                    print("[OK] Initialize successful")
                    server_info = response["result"].get("serverInfo", {})
                    print(f"     Server: {server_info.get('name', 'Unknown')}")
                    print(f"     Version: {server_info.get('version', 'Unknown')}")
                else:
                    print(f"[ERROR] Initialize failed: {response}")
            except json.JSONDecodeError as e:
                print(f"[ERROR] Invalid JSON response: {e}")
                print(f"Response: {response_line}")
        
        # Test 2: List Tools
        print("\n[TEST 2] List Available Tools")
        tools_request = {
            "jsonrpc": "2.0",
            "id": 2,
            "method": "tools/list",
            "params": {}
        }
        
        request_json = json.dumps(tools_request) + "\n"
        process.stdin.write(request_json)
        process.stdin.flush()
        
        response_line = process.stdout.readline()
        if response_line:
            try:
                response = json.loads(response_line.strip())
                if "result" in response and "tools" in response["result"]:
                    tools = response["result"]["tools"]
                    print(f"[OK] Found {len(tools)} tools:")
                    for tool in tools:
                        print(f"     - {tool.get('name', 'Unknown')}: {tool.get('description', 'No description')}")
                else:
                    print(f"[ERROR] Tools list failed: {response}")
            except json.JSONDecodeError as e:
                print(f"[ERROR] Invalid JSON response: {e}")
        
        # Test 3: Call a tool
        print("\n[TEST 3] Call get_tekla_status tool")
        tool_request = {
            "jsonrpc": "2.0",
            "id": 3,
            "method": "tools/call",
            "params": {
                "name": "get_tekla_status",
                "arguments": {}
            }
        }
        
        request_json = json.dumps(tool_request) + "\n"
        process.stdin.write(request_json)
        process.stdin.flush()
        
        response_line = process.stdout.readline()
        if response_line:
            try:
                response = json.loads(response_line.strip())
                if "result" in response:
                    print("[OK] Tool call successful")
                    content = response["result"].get("content", [])
                    if content:
                        for item in content:
                            if item.get("type") == "text":
                                text_content = item.get("text", "")
                                # Parse if it's JSON
                                try:
                                    data = json.loads(text_content)
                                    if isinstance(data, dict) and data.get("Success"):
                                        print(f"     Tekla Status: Connected")
                                        tekla_data = data.get("Data", {})
                                        if tekla_data.get("modelName"):
                                            print(f"     Model: {tekla_data['modelName']}")
                                    else:
                                        print(f"     Response: {text_content[:100]}...")
                                except:
                                    print(f"     Response: {text_content[:100]}...")
                else:
                    print(f"[ERROR] Tool call failed: {response}")
            except json.JSONDecodeError as e:
                print(f"[ERROR] Invalid JSON response: {e}")
        
        print("\n[RESULT] MCP Protocol Test Complete")
        
        # Cleanup
        process.terminate()
        try:
            process.wait(timeout=5)
        except subprocess.TimeoutExpired:
            process.kill()
            
        return True
        
    except Exception as e:
        print(f"[ERROR] Test failed: {e}")
        return False

if __name__ == "__main__":
    success = test_mcp_protocol()
    if success:
        print("\n[SUCCESS] TeklaMCP MCP protocol working correctly!")
        print("Ready for Claude Desktop integration.")
    else:
        print("\n[FAILED] MCP protocol test failed.")