#!/usr/bin/env python3
"""
Simplified TeklaMCP Server - Phase 1 Implementation
Clean, focused tools for Tekla operations
"""

import asyncio
import aiohttp
import subprocess
import sys
import os
import time
import atexit
from fastmcp import FastMCP
from pydantic import BaseModel, Field
from typing import Optional

# Initialize FastMCP server
mcp = FastMCP("TeklaMCP Phase 1 Server")

# HTTP backend configuration
TEKLA_SERVER_URL = "http://localhost:8081"

# Global process management
http_backend_process = None

def start_http_backend():
    """Start the C# HTTP backend server"""
    global http_backend_process
    
    try:
        # Get the directory of this script
        server_dir = os.path.dirname(os.path.abspath(__file__))
        
        print(f"Starting HTTP backend from: {server_dir}", file=sys.stderr)
        
        http_backend_process = subprocess.Popen(
            ["dotnet", "run", "--project", "TeklaMCPSimple.csproj"],
            cwd=server_dir,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True
        )
        
        # Wait for HTTP backend to be ready
        for attempt in range(20):  # 10 second timeout
            try:
                import requests
                response = requests.get(f"{TEKLA_SERVER_URL}/test", timeout=1)
                if response.status_code == 200:
                    print("HTTP backend ready", file=sys.stderr)
                    return True
            except:
                pass
            
            if http_backend_process.poll() is not None:
                # Process has terminated
                print("HTTP backend failed to start", file=sys.stderr)
                return False
                
            time.sleep(0.5)
        
        print("HTTP backend startup timeout", file=sys.stderr)
        return False
        
    except Exception as e:
        print(f"Error starting HTTP backend: {e}", file=sys.stderr)
        return False

def cleanup_http_backend():
    """Clean up HTTP backend process"""
    global http_backend_process
    if http_backend_process:
        try:
            http_backend_process.terminate()
            try:
                http_backend_process.wait(timeout=5)
            except subprocess.TimeoutExpired:
                http_backend_process.kill()
            print("HTTP backend stopped", file=sys.stderr)
        except Exception as e:
            print(f"Error stopping HTTP backend: {e}", file=sys.stderr)

# Register cleanup function
atexit.register(cleanup_http_backend)

# ==================== DATA MODELS ====================

class BeamParams(BaseModel):
    """Parameters for beam creation"""
    start_x: float = Field(description="Start point X coordinate")
    start_y: float = Field(description="Start point Y coordinate") 
    start_z: float = Field(description="Start point Z coordinate")
    end_x: float = Field(description="End point X coordinate")
    end_y: float = Field(description="End point Y coordinate")
    end_z: float = Field(description="End point Z coordinate")
    profile: str = Field(description="Beam profile (e.g., W21X50)")
    material: str = Field(default="A992", description="Material grade")

class ColumnParams(BaseModel):
    """Parameters for column creation"""
    x: float = Field(description="X coordinate")
    y: float = Field(description="Y coordinate")
    base_z: float = Field(default=0, description="Base Z coordinate")
    height: float = Field(description="Column height in mm")
    profile: str = Field(description="Column profile (e.g., W14X90)")
    material: str = Field(default="A992", description="Material grade")

class BraceParams(BaseModel):
    """Parameters for brace creation"""
    start_x: float = Field(description="Start point X coordinate")
    start_y: float = Field(description="Start point Y coordinate")
    start_z: float = Field(description="Start point Z coordinate")
    end_x: float = Field(description="End point X coordinate")
    end_y: float = Field(description="End point Y coordinate")
    end_z: float = Field(description="End point Z coordinate")
    profile: str = Field(description="Brace profile (e.g., L4X4X1/2)")
    material: str = Field(default="A992", description="Material grade")

class CustomOperationParams(BaseModel):
    """Parameters for custom Tekla operations"""
    code: str = Field(description="C# code to execute in Tekla environment")
    description: Optional[str] = Field(default=None, description="Optional description of the operation")

# ==================== HTTP HELPER ====================

async def call_endpoint(endpoint: str, method: str = "POST", payload=None):
    """
    Helper function to make HTTP requests to Tekla backend
    """
    try:
        async with aiohttp.ClientSession() as session:
            url = f"{TEKLA_SERVER_URL}{endpoint}"
            
            if method == "GET":
                async with session.get(url) as response:
                    return await response.json() if response.content_type == 'application/json' else await response.text()
            elif method == "POST":
                if isinstance(payload, str):
                    # For execute-code endpoint, send as plain text
                    async with session.post(url, data=payload, headers={"Content-Type": "text/plain"}) as response:
                        return await response.text()
                else:
                    # For structured endpoints, send as JSON
                    async with session.post(url, json=payload) as response:
                        return await response.json() if response.content_type == 'application/json' else await response.text()
                        
    except Exception as e:
        return {
            "success": False,
            "error": f"HTTP communication error: {str(e)}",
            "endpoint": endpoint
        }

# ==================== SPECIALIZED TOOLS ====================

@mcp.tool()
async def create_beam(params: BeamParams) -> dict:
    """
    Create a structural beam in Tekla Structures
    Uses generic /create-element endpoint as per Phase 1 architecture
    """
    payload = {
        "ElementType": "BEAM",
        "StartPoint": {"X": params.start_x, "Y": params.start_y, "Z": params.start_z},
        "EndPoint": {"X": params.end_x, "Y": params.end_y, "Z": params.end_z},
        "Profile": params.profile,
        "Material": params.material
    }
    return await call_endpoint("/create-element", payload=payload)

@mcp.tool()
async def create_column(params: ColumnParams) -> dict:
    """
    Create a vertical column in Tekla Structures  
    Uses generic /create-element endpoint as per Phase 1 architecture
    """
    payload = {
        "ElementType": "COLUMN",
        "StartPoint": {"X": params.x, "Y": params.y, "Z": params.base_z},
        "EndPoint": {"X": params.x, "Y": params.y, "Z": params.base_z + params.height},
        "Profile": params.profile,
        "Material": params.material
    }
    return await call_endpoint("/create-element", payload=payload)

@mcp.tool()
async def create_brace(params: BraceParams) -> dict:
    """
    Create a diagonal brace in Tekla Structures
    Uses generic /create-element endpoint as per Phase 1 architecture
    """
    payload = {
        "ElementType": "BRACE",
        "StartPoint": {"X": params.start_x, "Y": params.start_y, "Z": params.start_z},
        "EndPoint": {"X": params.end_x, "Y": params.end_y, "Z": params.end_z},
        "Profile": params.profile,
        "Material": params.material
    }
    return await call_endpoint("/create-element", payload=payload)

# ==================== UTILITY TOOLS ====================

@mcp.tool()
async def get_tekla_status() -> dict:
    """
    Check Tekla connection status and model information
    """
    return await call_endpoint("/tekla-status", method="GET")

@mcp.tool()
async def get_element_info(element_id: int = Field(description="Tekla element ID")) -> dict:
    """
    Get detailed information about a specific element
    """
    return await call_endpoint(f"/elements/{element_id}", method="GET")

@mcp.tool()
async def list_profiles(element_type: str = Field(description="Element type: BEAM, COLUMN, or BRACE")) -> dict:
    """
    Get list of available profiles for the specified element type
    """
    return await call_endpoint(f"/utils/profiles?type={element_type}", method="GET")

# ==================== CUSTOM OPERATION TOOL ====================

@mcp.tool()
async def custom_tekla_operation(params: CustomOperationParams) -> str:
    """
    Execute custom C# code in Tekla environment for experimental or non-standard operations.
    Use this for operations not covered by specialized tools.
    
    Example code:
    ```csharp
    var model = new Tekla.Structures.Model.Model();
    // Your custom operation here
    model.CommitChanges();
    return "Success";
    ```
    """
    # Add wrapper code if not present
    if not params.code.strip().startswith("var model"):
        wrapped_code = f"""
// Custom operation: {params.description or 'User-defined operation'}
var model = new Tekla.Structures.Model.Model();
if (!model.GetConnectionStatus()) {{
    return "ERROR: Tekla not connected";
}}

{params.code}

model.CommitChanges();
return "Custom operation completed successfully";
"""
        return await call_endpoint("/execute-code", payload=wrapped_code)
    else:
        return await call_endpoint("/execute-code", payload=params.code)

# ==================== BATCH OPERATION TOOL ====================

@mcp.tool()
async def create_multiple_elements(
    beams: list[BeamParams] = Field(default=[], description="List of beams to create"),
    columns: list[ColumnParams] = Field(default=[], description="List of columns to create"),
    braces: list[BraceParams] = Field(default=[], description="List of braces to create")
) -> dict:
    """
    Create multiple elements in a single batch operation.
    More efficient than creating elements one by one.
    """
    results = {
        "beams": [],
        "columns": [],
        "braces": [],
        "total_created": 0,
        "errors": []
    }
    
    # Create beams
    for beam in beams:
        result = await create_beam(beam)
        results["beams"].append(result)
        if result.get("success"):
            results["total_created"] += 1
        else:
            results["errors"].append(f"Beam error: {result.get('error')}")
    
    # Create columns
    for column in columns:
        result = await create_column(column)
        results["columns"].append(result)
        if result.get("success"):
            results["total_created"] += 1
        else:
            results["errors"].append(f"Column error: {result.get('error')}")
    
    # Create braces
    for brace in braces:
        result = await create_brace(brace)
        results["braces"].append(result)
        if result.get("success"):
            results["total_created"] += 1
        else:
            results["errors"].append(f"Brace error: {result.get('error')}")
    
    return results

# ==================== SERVER STARTUP ====================

if __name__ == "__main__":
    print("TeklaMCP Phase 1 Server - Starting...", file=sys.stderr)
    print(f"Backend URL: {TEKLA_SERVER_URL}", file=sys.stderr)
    
    # Start HTTP backend first
    print("Starting HTTP backend...", file=sys.stderr)
    if not start_http_backend():
        print("FAILED: Could not start HTTP backend. Tekla tools will not work.", file=sys.stderr)
        print("Make sure Tekla Structures is running and .NET SDK is installed.", file=sys.stderr)
        sys.exit(1)
    
    print("Available tools:", file=sys.stderr)
    print("  - create_beam: Create structural beams", file=sys.stderr)
    print("  - create_column: Create vertical columns", file=sys.stderr)
    print("  - create_brace: Create diagonal braces", file=sys.stderr)
    print("  - get_tekla_status: Check connection status", file=sys.stderr)
    print("  - get_element_info: Get element details", file=sys.stderr)
    print("  - list_profiles: List available profiles", file=sys.stderr)
    print("  - custom_tekla_operation: Execute custom C# code", file=sys.stderr)
    print("  - create_multiple_elements: Batch element creation", file=sys.stderr)
    print("", file=sys.stderr)
    print("TeklaMCP Server ready for Claude Desktop", file=sys.stderr)
    
    try:
        mcp.run()
    except KeyboardInterrupt:
        print("Shutting down TeklaMCP Server...", file=sys.stderr)
    except Exception as e:
        print(f"Server error: {e}", file=sys.stderr)
    finally:
        cleanup_http_backend()