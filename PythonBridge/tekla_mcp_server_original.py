#!/usr/bin/env python3
"""
Tekla MCP Server - Bridge between Claude and your .NET Tekla HTTP Server
This script translates Claude's MCP function calls into HTTP requests to your Tekla server.
"""

import asyncio
import json
import requests
from typing import Any, Dict, List

def convert_coordinates_if_needed(coord_string: str) -> str:
    """Convert comma-separated feet values to Tekla coordinate format if needed"""
    try:
        # Check if it's comma-separated numbers (feet)
        if ',' in coord_string and not ' ' in coord_string:
            feet_values = [float(x.strip()) for x in coord_string.split(',')]
            # Convert feet to mm and create Tekla format
            mm_values = [f"{x * 304.8:.2f}" for x in feet_values]
            return " ".join(mm_values)
        else:
            # Already in Tekla format or other valid format
            return coord_string
    except:
        # If conversion fails, return original
        return coord_string

# Configuration
TEKLA_SERVER_URL = "http://localhost:8080"

def make_http_request(endpoint: str, params: dict = None) -> str:
    """Make HTTP request to Tekla server and return formatted response"""
    try:
        url = f"{TEKLA_SERVER_URL}/{endpoint}"
        response = requests.get(url, params=params, timeout=10)
        
        if response.status_code == 200:
            result_text = response.text
            
            # Try to parse as JSON and make it more readable
            try:
                result_json = json.loads(result_text.replace("'", '"'))  # Fix single quotes
                if result_json.get("success"):
                    return f"SUCCESS: {result_json.get('message', 'Operation completed successfully')}"
                else:
                    return f"ERROR: {result_json.get('error', 'Operation failed')}"
            except:
                # If not valid JSON, return raw response
                return result_text
        else:
            return f"ERROR: HTTP Error {response.status_code}: Could not connect to Tekla server"
            
    except requests.exceptions.ConnectionError:
        return f"ERROR: Connection Error: Make sure your .NET Tekla server is running on {TEKLA_SERVER_URL}"
    except Exception as e:
        return f"ERROR: {str(e)}"

# Try to import MCP with fallback
try:
    from mcp.server import Server
    from mcp.server.stdio import stdio_server
    from mcp.types import Tool
    print("MCP imports successful")
    print("Server will listen for MCP connections...")  # Removed Unicode character
    
    # Initialize MCP Server
    server = Server("tekla-mcp")

    @server.list_tools()
    async def handle_list_tools():
        """List all available Tekla tools for Claude"""
        tools = [
            Tool(
                name="tekla_get_model_info",
                description="Get information about the current Tekla Structures model",
                inputSchema={
                    "type": "object",
                    "properties": {},
                    "required": []
                }
            ),
            Tool(
                name="tekla_create_grid",
                description="Create a comprehensive grid system in Tekla Structures. Supports both uniform spacing and custom coordinates.",
                inputSchema={
                    "type": "object",
                    "properties": {
                        "name": {
                            "type": "string",
                            "description": "Grid name/identifier",
                            "default": "Grid"
                        },
                        "x_coordinates": {
                            "type": "string",
                            "description": "X-axis coordinates. Format: '0.00 3*6096.00' (uniform) or '0.00 6096.00 12192.00 15240.00' (custom) or '25,30,42,35' (feet, comma-separated)",
                            "default": "0.00 2*6096.00"
                        },
                        "y_coordinates": {
                            "type": "string", 
                            "description": "Y-axis coordinates. Same format options as x_coordinates",
                            "default": "0.00 2*4572.00"
                        },
                        "z_coordinates": {
                            "type": "string",
                            "description": "Z-axis (elevation) coordinates. Format same as x_coordinates",
                            "default": "0.00"
                        },
                        "x_labels": {
                            "type": "string",
                            "description": "X-axis labels, space-separated (e.g., 'A B C D')",
                            "default": "A B C"
                        },
                        "y_labels": {
                            "type": "string",
                            "description": "Y-axis labels, space-separated (e.g., '1 2 3 4')",
                            "default": "1 2 3"
                        },
                        "z_labels": {
                            "type": "string",
                            "description": "Z-axis labels, space-separated (e.g., '+0 +12000 +24000')",
                            "default": "+0"
                        },
                        "extension_left_x": {
                            "type": "number",
                            "description": "Left extension for X grid lines in mm",
                            "default": 3048.0
                        },
                        "extension_right_x": {
                            "type": "number",
                            "description": "Right extension for X grid lines in mm",
                            "default": 3048.0
                        },
                        "extension_left_y": {
                            "type": "number",
                            "description": "Left extension for Y grid lines in mm",
                            "default": 3048.0
                        },
                        "extension_right_y": {
                            "type": "number",
                            "description": "Right extension for Y grid lines in mm",
                            "default": 3048.0
                        },
                        "is_magnetic": {
                            "type": "boolean",
                            "description": "Whether grid lines are magnetic (snap-to behavior)",
                            "default": False
                        }
                    },
                    "required": []
                }
            ),
            Tool(
                name="tekla_create_beam",
                description="Create a beam/column in Tekla Structures with comprehensive properties",
                inputSchema={
                    "type": "object",
                    "properties": {
                        "start_point": {
                            "type": "array",
                            "items": {"type": "number"},
                            "description": "Start point coordinates [x, y, z] in mm, or grid location like 'A-1' for auto-calculation",
                            "minItems": 3,
                            "maxItems": 3
                        },
                        "end_point": {
                            "type": "array", 
                            "items": {"type": "number"},
                            "description": "End point coordinates [x, y, z] in mm, or 'auto' for vertical column",
                            "minItems": 3,
                            "maxItems": 3
                        },
                        "grid_location": {
                            "type": "string",
                            "description": "Grid intersection (e.g., 'A-1', 'B-2') - alternative to start_point for columns"
                        },
                        "height": {
                            "type": "number",
                            "description": "Height for vertical elements (columns) in mm",
                            "default": 12000.0
                        },
                        "profile": {
                            "type": "string",
                            "description": "Beam/column profile (e.g., 'W12X50', 'HSS8X8X1/2', 'HEA400')",
                            "default": "W12X50"
                        },
                        "material": {
                            "type": "string",
                            "description": "Material grade (e.g., 'A992', 'A36', 'S355')",
                            "default": "A992"
                        },
                        "beam_class": {
                            "type": "string",
                            "description": "Class number for visualization/color (e.g., '1', '2', '3')",
                            "default": "1"
                        },
                        "finish": {
                            "type": "string",
                            "description": "Surface finish (e.g., 'PAINT', 'GALVANIZED', 'FIRE_PROTECTION')",
                            "default": "PAINT"
                        },
                        "name": {
                            "type": "string",
                            "description": "Beam name/identifier",
                            "default": "BEAM"
                        },
                        "position_rotation": {
                            "type": "string",
                            "description": "Beam rotation: 'FRONT', 'TOP', 'BACK', 'BELOW'",
                            "default": "FRONT"
                        },
                        "position_plane": {
                            "type": "string", 
                            "description": "Position plane: 'LEFT', 'MIDDLE', 'RIGHT'",
                            "default": "MIDDLE"
                        },
                        "position_depth": {
                            "type": "string",
                            "description": "Position depth: 'FRONT', 'MIDDLE', 'BEHIND'", 
                            "default": "MIDDLE"
                        },
                        "start_offset_dx": {
                            "type": "number",
                            "description": "Start point X offset in mm",
                            "default": 0.0
                        },
                        "start_offset_dy": {
                            "type": "number",
                            "description": "Start point Y offset in mm", 
                            "default": 0.0
                        },
                        "start_offset_dz": {
                            "type": "number",
                            "description": "Start point Z offset in mm",
                            "default": 0.0
                        },
                        "end_offset_dx": {
                            "type": "number",
                            "description": "End point X offset in mm",
                            "default": 0.0
                        },
                        "end_offset_dy": {
                            "type": "number",
                            "description": "End point Y offset in mm",
                            "default": 0.0
                        },
                        "end_offset_dz": {
                            "type": "number", 
                            "description": "End point Z offset in mm",
                            "default": 0.0
                        }
                    },
                    "required": []
                }
            ),
            Tool(
                name="tekla_create_column",
                description="Create a column at a specified grid intersection",
                inputSchema={
                    "type": "object",
                    "properties": {
                        "location": {
                            "type": "string",
                            "description": "Grid intersection location (e.g., 'A-1', 'B-2')",
                            "default": "A-1"
                        },
                        "profile": {
                            "type": "string",
                            "description": "AISC column profile (e.g., 'W12X50', 'W14X68')",
                            "default": "W12X50"
                        },
                        "height": {
                            "type": "number",
                            "description": "Column height in millimeters",
                            "default": 12000.0
                        }
                    },
                    "required": []
                }
            ),
            Tool(
                name="tekla_modify_column",
                description="Modify an existing column's profile at a specified grid location",
                inputSchema={
                    "type": "object",
                    "properties": {
                        "location": {
                            "type": "string",
                            "description": "Grid intersection location of the column to modify (e.g., 'A-1', 'B-2')"
                        },
                        "new_profile": {
                            "type": "string", 
                            "description": "New AISC profile for the column (e.g., 'W14X68', 'W16X77')"
                        }
                    },
                    "required": ["location", "new_profile"]
                }
            )
        ]
        return tools

    @server.call_tool()
    async def handle_call_tool(name: str, arguments: dict):
        """Handle tool calls from Claude and forward them to your .NET Tekla server"""
        
        # Route the function call to the appropriate endpoint
        if name == "tekla_get_model_info":
            result = make_http_request("model-info")
            
        elif name == "tekla_create_grid":
            params = {}
            
            # Handle coordinate formats (feet comma-separated, Tekla format, or default)
            x_coords = arguments.get("x_coordinates", "0.00 2*6096.00")
            y_coords = arguments.get("y_coordinates", "0.00 2*4572.00")
            z_coords = arguments.get("z_coordinates", "0.00")
            
            # Convert comma-separated feet to Tekla format if needed
            x_coords = convert_coordinates_if_needed(x_coords)
            y_coords = convert_coordinates_if_needed(y_coords)
            z_coords = convert_coordinates_if_needed(z_coords)
            
            params.update({
                "name": arguments.get("name", "Grid"),
                "x_coordinates": x_coords,
                "y_coordinates": y_coords, 
                "z_coordinates": z_coords,
                "x_labels": arguments.get("x_labels", "A B C"),
                "y_labels": arguments.get("y_labels", "1 2 3"),
                "z_labels": arguments.get("z_labels", "+0"),
                "extension_left_x": arguments.get("extension_left_x", 3048.0),
                "extension_right_x": arguments.get("extension_right_x", 3048.0),
                "extension_left_y": arguments.get("extension_left_y", 3048.0),
                "extension_right_y": arguments.get("extension_right_y", 3048.0),
                "is_magnetic": arguments.get("is_magnetic", False)
            })
            result = make_http_request("create-advanced-grid", params)
            
        elif name == "tekla_create_beam":
            params = {}
            
            # Handle grid location vs coordinates
            if arguments.get("grid_location"):
                params["grid_location"] = arguments.get("grid_location")
                params["height"] = arguments.get("height", 12000.0)
            elif arguments.get("start_point") and arguments.get("end_point"):
                start = arguments.get("start_point")
                end = arguments.get("end_point")
                params.update({
                    "start_x": start[0], "start_y": start[1], "start_z": start[2],
                    "end_x": end[0], "end_y": end[1], "end_z": end[2]
                })
            else:
                params.update({
                    "grid_location": "A-1", 
                    "height": arguments.get("height", 12000.0)
                })
            
            # Add all beam properties
            params.update({
                "profile": arguments.get("profile", "W12X50"),
                "material": arguments.get("material", "A992"),
                "beam_class": arguments.get("beam_class", "1"),
                "finish": arguments.get("finish", "PAINT"),
                "name": arguments.get("name", "BEAM"),
                "position_rotation": arguments.get("position_rotation", "FRONT"),
                "position_plane": arguments.get("position_plane", "MIDDLE"),
                "position_depth": arguments.get("position_depth", "MIDDLE"),
                "start_offset_dx": arguments.get("start_offset_dx", 0.0),
                "start_offset_dy": arguments.get("start_offset_dy", 0.0),
                "start_offset_dz": arguments.get("start_offset_dz", 0.0),
                "end_offset_dx": arguments.get("end_offset_dx", 0.0),
                "end_offset_dy": arguments.get("end_offset_dy", 0.0),
                "end_offset_dz": arguments.get("end_offset_dz", 0.0)
            })
            result = make_http_request("create-advanced-beam", params)
            
        elif name == "tekla_create_column":
            params = {
                "location": arguments.get("location", "A-1"),
                "profile": arguments.get("profile", "W12X50"),
                "height": arguments.get("height", 12000.0)
            }
            result = make_http_request("create-column", params)
            
        elif name == "tekla_modify_column":
            params = {
                "location": arguments.get("location"),
                "new_profile": arguments.get("new_profile")
            }
            if not params["location"] or not params["new_profile"]:
                result = "ERROR: Both 'location' and 'new_profile' are required for modify_column"
            else:
                result = make_http_request("modify-column", params)
                
        else:
            result = f"ERROR: Unknown tool: {name}"
        
        return [{"type": "text", "text": result}]

    async def main():
        """Start the MCP server"""
        log_file = "C:/Users/hugov/ClaudeProjects/2503_TeklaMCP/TeklaMCP.HttpServer/mcp_main_debug.log"
        try:
            with open(log_file, "w", encoding='utf-8') as f:
                f.write("Starting main() function...\n")
                f.flush()
                
            async with stdio_server() as (read_stream, write_stream):
                with open(log_file, "a", encoding='utf-8') as f:
                    f.write("stdio_server created successfully...\n")
                    f.flush()
                    
                # Add proper initialization options
                from mcp.server.models import InitializationOptions
                from mcp.server import NotificationOptions
                
                with open(log_file, "a", encoding='utf-8') as f:
                    f.write("About to start server.run()...\n")
                    f.flush()
                
                await server.run(
                    read_stream, 
                    write_stream,
                    InitializationOptions(
                        server_name="tekla-mcp",
                        server_version="1.0.0",
                        capabilities=server.get_capabilities(
                            notification_options=NotificationOptions(),
                            experimental_capabilities={}
                        )
                    )
                )
        except Exception as e:
            with open(log_file, "a", encoding='utf-8') as f:
                f.write(f"Error in main(): {e}\n")
                f.write(f"Error details: {traceback.format_exc()}\n")
                f.flush()
            print(f"Error starting MCP server: {e}")
            print("This might be normal - MCP servers run until Claude connects to them")

    MCP_AVAILABLE = True

except ImportError as e:
    print(f"❌ MCP import failed: {e}")
    print("Please install MCP: pip install mcp")
    MCP_AVAILABLE = False

    def main():
        print("❌ Cannot start MCP server - missing dependencies")
        return

if __name__ == "__main__":
    import sys
    import traceback
    
    # Create log file for debugging
    log_file = "C:/temp/tekla_mcp_debug.log"
    
    try:
        with open(log_file, "w", encoding='utf-8') as f:
            f.write("Starting Tekla MCP Server...\n")
            f.write("Make sure your .NET Tekla HTTP server is running on http://localhost:8080\n")
            f.flush()
        
        print("Starting Tekla MCP Server...")
        print("Make sure your .NET Tekla HTTP server is running on http://localhost:8080")
        print(f"Debug log location: {log_file}")
        
        if MCP_AVAILABLE:
            asyncio.run(main())
        else:
            with open(log_file, "a", encoding='utf-8') as f:
                f.write("ERROR: MCP not available - install with: pip install mcp\n")
            print("ERROR: MCP not available - install with: pip install mcp")
            
    except KeyboardInterrupt:
        with open(log_file, "a", encoding='utf-8') as f:
            f.write("\nTekla MCP Server stopped by user\n")
        print("\nTekla MCP Server stopped by user")
    except Exception as e:
        error_msg = f"ERROR: Failed to start: {e}\n{traceback.format_exc()}"
        with open(log_file, "a", encoding='utf-8') as f:
            f.write(error_msg + "\n")
        print(error_msg)
        print("\nTroubleshooting:")
        print("1. Install MCP: pip install mcp")
        print("2. Check Python version (3.8+ required)")  
        print("3. Make sure .NET server is running on localhost:8080")
        print(f"4. Check debug log: {log_file}")