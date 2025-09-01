# TeklaMCP Development Guidelines

## 1. CRITICAL: File Management
- **ONE server file:** `Server/TeklaMCP/server.py` - NEVER create duplicates
- **ONE HTTP backend:** `Server/TeklaMCP/ProgramClean.cs`
- **NO file renaming** - Both Claude Code & Desktop use same paths

## 2. Version Control Safety
- **Before major changes:** Create branch `git checkout -b backup/[date]`
- **Quick recovery:** `git checkout HEAD -- [file]`
- **View changes:** `git diff HEAD`
- **NEVER force push or rebase main branches**

## 3. Testing Protocol
- **Start HTTP server first:** `cd Server/TeklaMCP && dotnet run`
- **Test ALL MCP tools after changes** - not just subset
- **Verify both Claude Code and Desktop connections**
- **Document new tools in server.py docstrings**

## 4. Tekla API Reference
- **For class hierarchy and implementation priority:** See `@HugoNotes\07.Model-Object-Tekla-Info.md`
- **Priority objects marked with `*` in inheritance trees**
- **Key namespaces:** `Tekla.Structures.Model`, `Tekla.Structures.Geometry3d`

## 5. Architecture Principles
- **Direct API calls** - No unnecessary abstraction layers
- **Single responsibility** - Each endpoint does one thing
- **Model.CommitChanges()** after Insert/Modify operations
- **Error handling** with clear messages

## 6. Common Pitfalls to Avoid
- Creating multiple server versions
- Changing file paths without updating configs
- Missing Model.CommitChanges() after operations
- Testing only partial tool functionality