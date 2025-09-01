# Claude Desktop Integration Instructions for TeklaMCP

## Quick Setup (Recommended)

### Step 1: Update Claude Desktop Configuration

1. Open Claude Desktop configuration file:
   - Windows: `C:\Users\[USERNAME]\AppData\Roaming\Claude\claude_desktop_config.json`
   - Mac: `~/Library/Application Support/Claude/claude_desktop_config.json`

2. Update or add the TeklaMCP server configuration:

```json
{
  "mcpServers": {
    "tekla": {
      "command": "python",
      "args": [
        "C:\\Users\\hugov\\ClaudeProjects\\2510_TeklaMCP_0.3\\Server\\TeklaMCP\\start_mcp_server.py"
      ],
      "env": {
        "PYTHONIOENCODING": "utf-8"
      }
    }
  }
}
```

**Important:** Replace `C:\\Users\\hugov\\` with your actual username path.

### Step 2: Ensure Prerequisites

1. **Python 3.8+** installed and accessible via `python` command
2. **Tekla Structures** running with a model open
3. **FastMCP dependencies** installed:
   ```bash
   cd Server/TeklaMCP
   pip install -r requirements.txt
   ```

### Step 3: Test the Integration

1. **Restart Claude Desktop** completely (close and reopen)
2. **Start a new conversation**
3. **Test connection**: Ask Claude to "check Tekla status"

## Alternative Startup Methods

### Option 1: One-Click Startup (Windows)
Double-click `TeklaMCP_Start.bat` in the project root to start both servers automatically.

### Option 2: Manual Command Line
```bash
cd Server/TeklaMCP
python start_mcp_server.py
```

## What the Autostart Solution Does

The enhanced `start_mcp_server.py` automatically:

1. **Starts HTTP Backend (C#)**:
   - Handles all Tekla Structures API communication
   - Runs on port 8081
   - Provides REST endpoints for all operations

2. **Starts MCP Server (Python)**:
   - Provides tool definitions for Claude
   - Uses FastMCP framework for clean protocol handling
   - Communicates with HTTP backend via REST calls

3. **Manages Both Processes**:
   - Waits for HTTP backend to initialize
   - Provides graceful shutdown with Ctrl+C
   - Handles cleanup automatically

## Available Tools in Claude Desktop

Once configured, Claude will have access to these TeklaMCP tools:

| Tool | Description |
|------|-------------|
| `create_beam` | Create structural beams with start/end points and profile |
| `create_column` | Create vertical columns with position, height, and profile |
| `create_brace` | Create diagonal braces between two points |
| `get_tekla_status` | Check Tekla connection and get model information |
| `get_element_info` | Get detailed information about specific elements |
| `list_profiles` | Get available profiles for beam/column/brace types |
| `create_multiple_elements` | Batch create multiple elements efficiently |
| `custom_tekla_operation` | Execute custom C# code for advanced operations |

## Example Usage with Claude Desktop

After setup, you can ask Claude things like:

- "Check if Tekla is connected and show me the current model"
- "Create a beam from (0,0,0) to (5000,0,0) using W21X50 profile"
- "Create 5 columns in a row, each 3000mm high using W14X90"
- "Show me all available beam profiles"
- "Create a simple frame with beams and columns"

## Troubleshooting

### Server Won't Start
- Check that Tekla Structures is running with a model open
- Verify Python and .NET SDK are installed
- Check that ports 8081 is not in use by another application

### Claude Desktop Not Connecting  
- Restart Claude Desktop after configuration changes
- Check the configuration file path is correct
- Verify the Python path in the configuration matches your system

### Tools Not Available
- Wait 10-15 seconds after starting Claude Desktop for MCP connection
- Check that both servers started successfully (look for "System ready!" message)
- Restart the conversation in Claude Desktop

## Architecture Overview

```
Claude Desktop
    ↓ (MCP Protocol)
FastMCP Server (Python)
    ↓ (HTTP REST calls)
HTTP Backend (C#)
    ↓ (Direct API calls)
Tekla Structures API
```

This architecture ensures:
- **Clean separation** between MCP tools and Tekla communication
- **Proven reliability** using tested V2 connection sequence  
- **Easy maintenance** with modular, focused components
- **Automatic startup** with single command convenience

## Success Indicators

When working correctly, you should see:
1. Both servers start with the autostart script
2. "System ready! Both servers running." message
3. Claude Desktop shows TeklaMCP tools available
4. Tool calls return proper Tekla model information