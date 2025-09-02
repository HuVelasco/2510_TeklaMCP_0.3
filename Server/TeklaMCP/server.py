#!/usr/bin/env python3
"""
TeklaMCP Server 0.3 - PHASE 2: Comprehensive CRUD Operations
Bridge between Claude Code and TeklaMCP HTTP Server

PHASE 1 IMPROVEMENTS:
- Eliminated 54 lines of HTTP request duplication
- Standardized error handling patterns
- Modularized connection verification
- Cleaned up duplicate files

PHASE 2 IMPROVEMENTS:
- Complete CRUD operations (Create/Read/Update/Delete)
- Advanced search and filtering capabilities
- Bulk operations support
- Comprehensive object lifecycle management
"""

import asyncio
import aiohttp
import sys
import subprocess
import os
from fastmcp import FastMCP
from pydantic import BaseModel, Field
from typing import Optional

# Initialize FastMCP server
mcp = FastMCP("TeklaMCP Server")

# HTTP backend configuration
TEKLA_SERVER_URL = "http://localhost:8080"

# ==================== HTTP SERVER AUTOMATION ====================

async def ensure_http_server_running():
    """Automatically detect and start HTTP server if not running (DEVELOPMENT ONLY)"""
    try:
        # Quick health check with short timeout
        async with aiohttp.ClientSession() as session:
            async with session.get(f"{TEKLA_SERVER_URL}/test", timeout=aiohttp.ClientTimeout(total=2)) as response:
                if response.status == 200:
                    return True  # Server is running
    except:
        # Server not running - start it for development
        print(f"HTTP server not detected on {TEKLA_SERVER_URL}")
        print("Auto-starting HTTP server for development...")
        
        try:
            # Get the HTTP server directory (development setup)
            current_dir = os.path.dirname(__file__)
            server_path = current_dir  # Server/TeklaMCP directory
            
            # Start HTTP server in new console window (Windows development)
            if os.name == 'nt':  # Windows
                subprocess.Popen(
                    ["dotnet", "run"], 
                    cwd=server_path,
                    creationflags=subprocess.CREATE_NEW_CONSOLE
                )
            else:  # Linux/Mac (if needed)
                subprocess.Popen(
                    ["dotnet", "run"], 
                    cwd=server_path
                )
            
            # Give HTTP server time to start
            print("Waiting for HTTP server startup...")
            await asyncio.sleep(4)
            
            # Verify it started successfully
            async with aiohttp.ClientSession() as session:
                async with session.get(f"{TEKLA_SERVER_URL}/test", timeout=aiohttp.ClientTimeout(total=3)) as response:
                    if response.status == 200:
                        print("HTTP server started successfully!")
                        return True
            
            print("HTTP server failed to start properly")
            return False
            
        except Exception as e:
            print(f"Failed to auto-start HTTP server: {e}")
            print("Please manually start: cd Server/TeklaMCP && dotnet run")
            return False
    
    return False

# ==================== HTTP HELPER ====================

async def make_http_request(session, method: str, url: str, endpoint: str, params=None):
    """Core HTTP request logic - eliminated duplication"""
    if method == "GET":
        async with session.get(url) as response:
            return response.status, await response.text()
    
    elif method == "POST":
        # Special handling for execute-code endpoint - send as plain text
        if endpoint == "execute-code" and isinstance(params, str):
            async with session.post(url, data=params, headers={"Content-Type": "text/plain"}) as response:
                return response.status, await response.text()
        else:
            # For other endpoints, send as JSON
            async with session.post(url, json=params) as response:
                return response.status, await response.text()
    
    elif method == "PUT":
        # Handle PUT requests for updates
        if isinstance(params, str):
            async with session.put(url, data=params, headers={"Content-Type": "application/json"}) as response:
                return response.status, await response.text()
        else:
            async with session.put(url, json=params) as response:
                return response.status, await response.text()
    
    elif method == "DELETE":
        # Handle DELETE requests
        async with session.delete(url) as response:
            return response.status, await response.text()

async def call_endpoint(endpoint: str, method: str = "GET", params=None):
    """Helper function to make HTTP requests to Tekla backend with auto-start capability"""
    
    # Ensure HTTP server is running (auto-start for development)
    await ensure_http_server_running()
    
    try:
        async with aiohttp.ClientSession() as session:
            url = f"{TEKLA_SERVER_URL}/{endpoint}"
            status, result = await make_http_request(session, method, url, endpoint, params)
            
            if status == 200:
                return f"SUCCESS: {result}"
            else:
                return f"ERROR: HTTP {status}: {result}"
                        
    except aiohttp.ClientError as e:
        # Try auto-start one more time on connection failure
        print("Connection failed, attempting HTTP server restart...")
        if await ensure_http_server_running():
            # Retry the request once after auto-start
            try:
                async with aiohttp.ClientSession() as session:
                    url = f"{TEKLA_SERVER_URL}/{endpoint}"
                    status, result = await make_http_request(session, method, url, endpoint, params)
                    
                    if status == 200:
                        return f"SUCCESS: {result}"
                    else:
                        return f"ERROR: HTTP {status}: {result}"
            except:
                pass
        
        return f"ERROR: Connection failed - {e}. Auto-start attempted but failed. Please manually start: cd Server/TeklaMCP && dotnet run"
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
    position_depth: str = "BEHIND",    # FRONT, MIDDLE, BEHIND
    position_plane: str = "MIDDLE",    # LEFT, MIDDLE, RIGHT 
    position_rotation: str = "TOP",    # FRONT, TOP, BACK, BELOW
    class_value: str = "1",
    name: str = "BEAM"
) -> str:
    """Create a beam with full position control - calls HTTP beam endpoint"""
    # Parse coordinates
    start_coords = start_point.split()
    end_coords = end_point.split()
    
    # Build BeamRequest payload
    beam_request = {
        "StartPointX": float(start_coords[0]),
        "StartPointY": float(start_coords[1]),
        "StartPointZ": float(start_coords[2]),
        "EndPointX": float(end_coords[0]),
        "EndPointY": float(end_coords[1]),
        "EndPointZ": float(end_coords[2]),
        "Profile": profile,
        "Material": material,
        "Class": class_value,
        "Name": name,
        "PositionDepth": position_depth,
        "PositionPlane": position_plane,
        "PositionRotation": position_rotation
    }
    
    return await call_endpoint("create-beam", "POST", beam_request)



@mcp.tool()
async def tekla_create_column(
    base_point: str,   # "3000 3000 0"
    height: str = "3000",
    profile: str = "W14X90",
    material: str = "A992",
    position_depth: str = "MIDDLE",
    position_plane: str = "MIDDLE",     # MIDDLE for columns (centered)
    position_rotation: str = "TOP",     # TOP for columns (centered)
    class_value: str = "1",
    name: str = "COLUMN"
) -> str:
    """Create vertical column using beam endpoint - calls HTTP beam endpoint"""
    # Parse base point coordinates
    base_coords = base_point.split()
    height_val = float(height)
    
    # Calculate end point (vertical: same x,y but z + height)
    end_x = float(base_coords[0])
    end_y = float(base_coords[1]) 
    end_z = float(base_coords[2]) + height_val
    
    # Build BeamRequest payload with column-specific defaults
    beam_request = {
        "StartPointX": float(base_coords[0]),
        "StartPointY": float(base_coords[1]),
        "StartPointZ": float(base_coords[2]),
        "EndPointX": end_x,
        "EndPointY": end_y,
        "EndPointZ": end_z,
        "Profile": profile,
        "Material": material,
        "Class": class_value,
        "Name": name,
        "PositionDepth": position_depth,
        "PositionPlane": position_plane,
        "PositionRotation": position_rotation
    }
    
    return await call_endpoint("create-beam", "POST", beam_request)


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

# ==================== PHASE 2: COMPREHENSIVE CRUD OPERATIONS ====================

@mcp.tool()
async def tekla_list_objects(
    object_type: str = "ALL",  # BEAM, COLUMN, CONNECTION, ALL
    limit: int = 100,
    offset: int = 0,
    filter_criteria: str = ""  # "profile:W18" or similar
) -> str:
    """List objects by type with optional filtering
    
    PHASE 2 ENHANCEMENT: Comprehensive object listing with filtering
    
    Parameters:
    - object_type: Type of objects to list (BEAM, COLUMN, CONNECTION, ALL)
    - limit: Maximum number of objects to return (default: 100)
    - offset: Number of objects to skip (default: 0)
    - filter_criteria: Optional filter criteria in format "property:value"
    
    Returns objects with standardized Phase 2 response format
    """
    endpoint = f"objects/{object_type}?limit={limit}&offset={offset}"
    if filter_criteria:
        endpoint += f"&filter={filter_criteria}"
    
    return await call_endpoint(endpoint, "GET")

@mcp.tool()
async def tekla_search_objects(
    query: str,
    object_types: str = "BEAM,COLUMN",  # Comma-separated types
    search_properties: str = "profile,material",  # Comma-separated properties
    limit: int = 50
) -> str:
    """Search objects with advanced criteria across multiple properties
    
    PHASE 2 ENHANCEMENT: Advanced search functionality
    
    Parameters:
    - query: Search query to match against object properties
    - object_types: Comma-separated list of object types to search
    - search_properties: Comma-separated list of properties to search in
    - limit: Maximum number of results to return
    
    Returns matching objects with search relevance information
    """
    params = {
        "q": query,
        "types": object_types,
        "properties": search_properties,
        "limit": limit
    }
    
    return await call_endpoint("objects/search", "GET", params)

@mcp.tool()
async def tekla_get_object_details(
    object_id: int
) -> str:
    """Get comprehensive details about a specific object
    
    PHASE 2 ENHANCEMENT: Detailed object information retrieval
    
    Parameters:
    - object_id: Tekla object ID to retrieve details for
    
    Returns complete object information including type-specific properties
    """
    return await call_endpoint(f"object/{object_id}", "GET")

@mcp.tool()
async def tekla_update_object(
    object_id: int,
    updates: str  # JSON string with properties to update
) -> str:
    """Update properties of an existing object
    
    PHASE 2 ENHANCEMENT: Object modification capabilities
    
    Parameters:
    - object_id: ID of the object to update
    - updates: JSON string containing properties to update
    
    Example updates format:
    '{"profile": "W21X50", "material": "A992", "class": "2"}'
    
    Returns update confirmation with applied changes
    """
    return await call_endpoint(f"object/{object_id}", "PUT", updates)

@mcp.tool()
async def tekla_delete_object(
    object_id: int
) -> str:
    """Delete an object from the model
    
    PHASE 2 ENHANCEMENT: Object deletion capabilities
    
    Parameters:
    - object_id: ID of the object to delete
    
    WARNING: This permanently removes the object from the model!
    
    Returns deletion confirmation or error details
    """
    return await call_endpoint(f"object/{object_id}", "DELETE")

@mcp.tool()
async def tekla_bulk_operations(
    operations: str  # JSON array of operations
) -> str:
    """Execute multiple operations in a single transaction
    
    PHASE 2 ENHANCEMENT: Bulk operations for efficiency
    
    Parameters:
    - operations: JSON array of operations to execute
    
    Example operations format:
    '[
        {"type": "create_beam", "data": {"start_point": "0 0 0", "end_point": "5000 0 0"}},
        {"type": "update", "data": {"object_id": 12345, "profile": "W18X35"}},
        {"type": "delete", "data": {"object_id": 67890}}
    ]'
    
    Returns summary of all operations with individual results
    """
    return await call_endpoint("objects/bulk", "POST", operations)

# ==================== MAIN ====================

if __name__ == "__main__":
    mcp.run()