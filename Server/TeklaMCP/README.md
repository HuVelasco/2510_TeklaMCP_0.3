# TeklaMCP Production Server

## Architecture

This is the **Phase 1 Production Server** following clean controller architecture:

```
FastMCP Python Server → HTTP Server (C#) → Controllers → Tekla API
```

## File Structure

```
Server/TeklaMCP/
├── mcp_server_simplified.py    # Production FastMCP server
├── start_mcp_server.py         # Startup script with config
├── HttpServer.cs               # C# HTTP backend
├── Controllers/
│   ├── ElementsController.cs   # Element creation (beams, columns, braces)
│   └── UtilsController.cs      # Status, profiles, code execution
├── Models/
│   └── RequestModels.cs        # Data transfer objects
├── config/
│   └── mcp_config.json        # Server configuration
└── requirements.txt           # Python dependencies
```

## Quick Start

### 1. Start HTTP Backend
```bash
cd Server/TeklaMCP
dotnet run --project TeklaMCPSimple.csproj
```

### 2. Start MCP Server
```bash
cd Server/TeklaMCP
pip install -r requirements.txt
python start_mcp_server.py
```

## Available Tools

### Specialized Tools
- `create_beam(start_x, start_y, start_z, end_x, end_y, end_z, profile, material)`
- `create_column(x, y, base_z, height, profile, material)`
- `create_brace(start_x, start_y, start_z, end_x, end_y, end_z, profile, material)`

### Utility Tools
- `get_tekla_status()` - Check connection and model info
- `get_element_info(element_id)` - Get element properties
- `list_profiles(element_type)` - Available profiles for element type

### Custom Operations
- `custom_tekla_operation(code, description)` - Execute custom C# code
- `create_multiple_elements(beams, columns, braces)` - Batch operations

## Configuration

Edit `config/mcp_config.json` to customize:
- Backend URL and timeout settings
- Tool availability
- Logging configuration

## Phase 1 Endpoints

The HTTP backend provides these RESTful endpoints:

- `POST /create-element` - Generic element creation
- `GET /elements/{id}` - Element information
- `GET /tekla-status` - Connection status
- `GET /test` - Health check
- `POST /execute-code` - Custom code execution
- `GET /utils/profiles` - Available profiles

## Development vs Production

- **Production Code**: This folder (`Server/TeklaMCP/`)
- **Experiments**: See `experiments/` folder in project root
- **Documentation**: See project root for architecture analysis

## Phase 2 Roadiness

This Phase 1 implementation is ready for expansion with:
- Connection tools
- Workflow operations
- Import/export functionality
- Advanced batch operations