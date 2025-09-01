#!/usr/bin/env python3
"""
Modern TeklaMCP Server - Updated for current MCP protocol
Bridge between Claude Code and TeklaMCP HTTP Server
"""

import asyncio
import aiohttp
import sys
from fastmcp import FastMCP
from pydantic import BaseModel, Field
from typing import Optional

# Initialize FastMCP server
mcp = FastMCP("TeklaMCP Server")

# HTTP backend configuration
TEKLA_SERVER_URL = "http://localhost:8080"

# ==================== HTTP HELPER ====================

async def call_endpoint(endpoint: str, method: str = "GET", params=None):
    """Helper function to make HTTP requests to Tekla backend"""
    try:
        async with aiohttp.ClientSession() as session:
            url = f"{TEKLA_SERVER_URL}/{endpoint}"
            
            if method == "GET":
                async with session.get(url, params=params) as response:
                    if response.status == 200:
                        result = await response.text()
                        return f"SUCCESS: {result}"
                    else:
                        return f"ERROR: HTTP {response.status}: {await response.text()}"
            
            elif method == "POST":
                # Special handling for execute-code endpoint - send as plain text
                if endpoint == "execute-code" and isinstance(params, str):
                    async with session.post(url, data=params, headers={"Content-Type": "text/plain"}) as response:
                        if response.status == 200:
                            result = await response.text()
                            return f"SUCCESS: {result}"
                        else:
                            return f"ERROR: HTTP {response.status}: {await response.text()}"
                else:
                    # For other endpoints, send as JSON
                    async with session.post(url, json=params) as response:
                        if response.status == 200:
                            result = await response.text()
                            return f"SUCCESS: {result}"
                        else:
                            return f"ERROR: HTTP {response.status}: {await response.text()}"
                        
    except aiohttp.ClientError as e:
        return f"ERROR: Connection failed - {e}. Make sure HTTP server is running on {TEKLA_SERVER_URL}"
    except Exception as e:
        return f"ERROR: Unexpected error - {e}"

# ==================== MCP TOOLS ====================

@mcp.tool()
async def tekla_get_model_info() -> str:
    """Get information about the current Tekla Structures model"""
    return await call_endpoint("tekla-info", "GET")


@mcp.tool()
async def tekla_create_beam(
    start_point: str,  # "0 0 0"
    end_point: str,    # "6000 0 0"
    profile: str = "W21X50",
    material: str = "A992",
    position_depth: str = "MIDDLE",    # FRONT, MIDDLE, BEHIND
    position_plane: str = "MIDDLE",    # LEFT, MIDDLE, RIGHT
    position_rotation: str = "FRONT"   # FRONT, TOP, BACK, BELOW
) -> str:
    """Create a beam with full position control using Tekla enums"""
    # Parse coordinates
    start_coords = start_point.split()
    end_coords = end_point.split()
    
    # Generate C# code with proper Position.DepthEnum syntax
    code = f'''
var beam = new Tekla.Structures.Model.Beam();
beam.StartPoint = new Tekla.Structures.Geometry3d.Point({start_coords[0]}, {start_coords[1]}, {start_coords[2]});
beam.EndPoint = new Tekla.Structures.Geometry3d.Point({end_coords[0]}, {end_coords[1]}, {end_coords[2]});
beam.Profile.ProfileString = "{profile}";
beam.Material.MaterialString = "{material}";
beam.Name = "BEAM";
beam.Class = "1";

beam.Position.Depth = Tekla.Structures.Model.Position.DepthEnum.{position_depth.upper()};
beam.Position.Plane = Tekla.Structures.Model.Position.PlaneEnum.{position_plane.upper()};  
beam.Position.Rotation = Tekla.Structures.Model.Position.RotationEnum.{position_rotation.upper()};

if (beam.Insert())
{{
    var teklaModel = new Tekla.Structures.Model.Model();
    teklaModel.CommitChanges();
    return "SUCCESS: Beam created with ID " + beam.Identifier.ID.ToString() + " - Profile: {profile}, Position: {position_depth}/{position_plane}/{position_rotation}";
}}
else
{{
    return "ERROR: Failed to create beam";
}}
'''
    
    return await call_endpoint("execute-code", "POST", code)



@mcp.tool()
async def tekla_execute_code(
    code: str
) -> str:
    """Execute custom C# code in Tekla environment for advanced operations
    
    IMPORTANT: Uses legacy C# compiler (C# 5.0 syntax only)
    
    SYNTAX REQUIREMENTS:
    ❌ NO 'using' statements - use fully qualified names
    ❌ NO string interpolation: $"{variable}"
    ✅ USE string concatenation: "text " + variable
    ❌ NO null-conditional: obj?.Property
    ✅ USE: (obj != null) ? obj.Property : defaultValue
    
    CORRECT EXAMPLE:
    ```csharp
    // Create a column (vertical beam)
    var p1 = new Tekla.Structures.Geometry3d.Point(0, 0, 0);
    var p2 = new Tekla.Structures.Geometry3d.Point(0, 0, 4000);
    
    var column = new Tekla.Structures.Model.Beam();
    column.StartPoint = p1;
    column.EndPoint = p2;
    column.Profile.ProfileString = "W14X90";
    column.Material.MaterialString = "A992";
    
    if (column.Insert()) {
        new Tekla.Structures.Model.Model().CommitChanges();
        return "Column created with ID: " + column.Identifier.ID.ToString();
    }
    return "Failed to create column";
    ```
    
    WRONG EXAMPLE (will fail):
    ```csharp
    using Tekla.Structures.Model;  // ❌ NO using statements!
    using Tekla.Structures.Geometry3d;
    
    Point p1 = new Point(0, 0, 0);  // ❌ Won't work without full namespace
    return $"ID: {column.ID}";      // ❌ No string interpolation
    ```
    """
    # Send the C# code as plain text to the execute-code endpoint
    return await call_endpoint("execute-code", "POST", code)

# ==================== MAIN ====================

if __name__ == "__main__":
    mcp.run()