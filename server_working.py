#!/usr/bin/env python3
"""
TeklaMCP Phase 1 Server - Clean MCP Implementation
Direct STDIO communication - no wrapper scripts
Assumes HTTP backend is started externally to avoid MCP conflicts
"""

import asyncio
import aiohttp
import sys
from fastmcp import FastMCP
from pydantic import BaseModel, Field
from typing import Optional

# Initialize FastMCP server
mcp = FastMCP("TeklaMCP Phase 1 Clean Server")

# HTTP backend configuration - assumes external startup
TEKLA_SERVER_URL = "http://localhost:8081"

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


# ==================== SERVER STARTUP ====================

if __name__ == "__main__":
    # Clean MCP startup - no subprocess management
    print("TeklaMCP Phase 1 Clean Server - Starting...", file=sys.stderr)
    print(f"Backend URL: {TEKLA_SERVER_URL}", file=sys.stderr)
    print("", file=sys.stderr)
    print("IMPORTANT: Start HTTP backend manually with:", file=sys.stderr) 
    print("  dotnet run --project TeklaMCPSimple.csproj", file=sys.stderr)
    print("", file=sys.stderr)
    print("Phase 1 Available tools:", file=sys.stderr)
    print("  - create_beam: Create structural beams", file=sys.stderr)
    print("  - create_column: Create vertical columns", file=sys.stderr) 
    print("  - get_tekla_status: Check connection status", file=sys.stderr)
    print("  - get_element_info: Get element details", file=sys.stderr)
    print("  - list_profiles: List available profiles", file=sys.stderr)
    print("  - custom_tekla_operation: Execute custom C# code", file=sys.stderr)
    print("", file=sys.stderr)
    print("TeklaMCP Clean Server ready for Claude Desktop MCP integration", file=sys.stderr)
    
    try:
        mcp.run()
    except KeyboardInterrupt:
        print("Shutting down TeklaMCP Clean Server...", file=sys.stderr)
    except Exception as e:
        print(f"Server error: {e}", file=sys.stderr)