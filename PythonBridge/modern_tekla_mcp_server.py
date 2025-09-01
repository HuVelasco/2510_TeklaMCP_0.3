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
    start_point: str,
    end_point: str,
    profile: str = "W21X50",
    material: str = "A992"
) -> str:
    """Create a beam in Tekla Structures with comprehensive properties"""
    params = {
        "start": start_point,
        "end": end_point,
        "profile": profile,
        "material": material
    }
    return await call_endpoint("create-beam", "POST", params)



@mcp.tool()
async def tekla_execute_code(
    code: str
) -> str:
    """Execute custom C# code in Tekla environment for advanced operations"""
    # Send the C# code as plain text to the execute-code endpoint
    return await call_endpoint("execute-code", "POST", code)

# ==================== MAIN ====================

if __name__ == "__main__":
    mcp.run()