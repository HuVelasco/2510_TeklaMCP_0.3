# tekla-mcp

MCP server for AI control of Tekla Structures — 3D structural modeling, drawing automation, and BIM workflows.

**By [Hugo Velasco](https://github.com/HuVelasco)** - [dualitum.com](https://dualitum.com)

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE.txt)
[![Python](https://img.shields.io/badge/Python-3.11%2B-blue)](https://python.org)
[![MCP](https://img.shields.io/badge/MCP-FastMCP-green)](https://github.com/jlowin/fastmcp)
[![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey)](https://www.tekla.com)

## Overview

Tekla MCP connects Claude (or any MCP-compatible AI client) directly to Tekla Structures via the Tekla Open API, enabling AI-driven 3D modeling, member creation, drawing extraction, and BIM automation.

## Architecture

```
Claude / AI Client
       | MCP (stdio)
       v
  Python FastMCP Server (server.py)
       | Tekla Open API (.NET)
       v
  C# Bridge (TeklaMCPClean.csproj)
       |
  Tekla Structures (running instance)
```

## Requirements

- Windows (Tekla Structures is Windows-only)
- Tekla Structures (any recent version with Open API support)
- Python 3.11+
- .NET SDK (for C# bridge compilation)

## Installation

```bash
cd Server/TeklaMCP
pip install -r requirements.txt
```

Build the C# bridge:
```bash
dotnet build TeklaMCPClean.csproj
```

## Usage

Add to your Claude Desktop `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "tekla-mcp": {
      "command": "python",
      "args": ["path/to/Server/TeklaMCP/server.py"]
    }
  }
}
```

Tekla Structures must be open and running before starting the server.

## Capabilities

- **3D Modeling**: Create and modify beams, columns, braces, and connections
- **Drawing Automation**: Extract and manage Tekla drawings
- **Model Query**: List and search model objects, get geometry and properties
- **BIM Workflows**: Export/import structural data

## File Structure

```
Server/TeklaMCP/
  server.py               # MCP server entry point (Python/FastMCP)
  ProgramClean.cs         # C# bridge to Tekla Open API
  TeklaMCPClean.csproj    # C# project file
  requirements.txt        # Python dependencies
  debug_tekla_registry.py # Utility: debug Tekla API registry
```

---

*Copyright 2026 Hugo Velasco. All rights reserved.*