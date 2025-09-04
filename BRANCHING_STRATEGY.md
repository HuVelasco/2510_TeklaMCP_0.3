# TeklaMCP Branching Strategy

## Overview
This document defines the branching strategy for the TeklaMCP project, establishing clear guidelines for development, testing, and production releases.

## Branch Types

### Production Branches
- **`production/v*`** - Stable production releases
  - Example: `production/v0.3.0`
  - Protected branches requiring pull request reviews
  - Only hotfixes and critical updates allowed
  - Automatically tagged when merged

### Development Branches
- **`master`** - Main development branch
  - Latest stable development code
  - All feature branches merge here first
  - Continuous integration testing required

- **`improvements_*`** - Feature development branches
  - Pattern: `improvements_[topic]_[phase]_[description]`
  - Examples:
    - `improvements_crud_02_operations`
    - `improvements_architecture_03_handlers`
    - `improvements_deduplication_01_code_cleanup`
  - Used for incremental improvements and new features

### Feature Branches
- **`feature/*`** - Experimental and major feature development
  - Examples:
    - `feature/modernize-mcp-server`
    - `feature/part-based-tools-Phase.01`
    - `feature/real-dynamic-execution`
  - Used for experimental work and major architectural changes

### Backup Branches
- **`backup/*`** - Snapshot branches for important milestones
  - Example: `backup/phase1-working`
  - Used to preserve working states before major refactoring

## Workflow

### Development Workflow
1. **Create Feature Branch**
   ```bash
   git checkout master
   git pull origin master
   git checkout -b improvements_[topic]_[phase]_[description]
   ```

2. **Development Process**
   - Make incremental commits with detailed messages
   - Test functionality at each commit
   - Document changes in commit messages
   - Include impact assessment in commits

3. **Integration**
   ```bash
   git checkout master
   git merge improvements_[topic]_[phase]_[description]
   git push origin master
   ```

### Production Release Workflow
1. **Create Production Branch**
   ```bash
   git checkout master
   git checkout -b production/v[MAJOR].[MINOR].[PATCH]
   ```

2. **Final Testing and Cleanup**
   - Verify all features working
   - Clean up temporary files
   - Update documentation
   - Run full test suite

3. **Release Tagging**
   ```bash
   git tag -a v[MAJOR].[MINOR].[PATCH] -m "Production Release v[MAJOR].[MINOR].[PATCH]"
   git push origin production/v[MAJOR].[MINOR].[PATCH]
   git push origin v[MAJOR].[MINOR].[PATCH]
   ```

## Version Numbering

### Semantic Versioning (SemVer)
Format: `MAJOR.MINOR.PATCH`

- **MAJOR**: Breaking changes to MCP tool signatures or architecture
- **MINOR**: New features, tools, or significant improvements
- **PATCH**: Bug fixes, documentation updates, minor improvements

### Current Version: v0.3.0
- **0**: Pre-production major version
- **3**: Third minor release with significant features
- **0**: Initial release of this feature set

## Branch Protection Rules

### Production Branches (`production/*`)
- Require pull request reviews (minimum 1)
- Require status checks to pass
- Restrict push to administrators only
- Do not allow force pushes
- Do not allow deletions

### Master Branch
- Require status checks to pass
- Require branches to be up to date before merging
- Allow administrators to bypass requirements (for hotfixes)

## Commit Standards

### Commit Message Format
```
[TYPE]: [Brief description]

[Detailed explanation of changes]
[Impact assessment]
[Test results]

Co-authored-by: [if applicable]
```

### Commit Types
- **PHASE [N] COMPLETE**: Major milestone completion
- **FIX**: Bug fixes and corrections
- **FEATURE**: New functionality implementation
- **REFACTOR**: Code structure improvements
- **DOCS**: Documentation updates
- **TEST**: Testing improvements
- **CONFIG**: Configuration changes

### Example Good Commit
```
PHASE 4 COMPLETE: Part-Based CRUD Operations

- Implemented comprehensive part filtering and search
- Added bulk operations for parts management
- Enhanced error handling with detailed context
- Reduced code complexity through handler pattern

Testing: All HTTP endpoints verified working
Impact: 66% code reduction (1587→535 lines)
Tools: 15 MCP tools now fully operational

Co-authored-by: Claude <claude@anthropic.com>
```

## Testing Strategy

### Branch Testing Requirements
- **Feature Branches**: Manual testing required before merge
- **Master**: Automated CI testing (when available)
- **Production**: Full integration testing required

### Testing Checklist
- [ ] HTTP server starts without errors
- [ ] All MCP tools respond correctly
- [ ] Tekla integration functional
- [ ] No breaking changes to existing tools
- [ ] Documentation updated

## Release Notes

### v0.3.0 Features
- Complete CRUD operations for Tekla elements
- Part-based filtering and search capabilities
- Clean architecture with endpoint-specific handlers
- 15 fully functional MCP tools
- Comprehensive error handling
- Agent-friendly tool design

### Previous Releases
- **v0.2.x**: CRUD operations implementation
- **v0.1.x**: Foundation and basic MCP integration

## Emergency Procedures

### Hotfix Process
1. Create hotfix branch from production branch
2. Apply minimal fix with thorough testing
3. Merge to both production and master
4. Create patch release tag

### Rollback Process
1. Identify last known good commit/tag
2. Create rollback branch
3. Cherry-pick necessary fixes
4. Deploy as emergency release

---

*Last Updated: 2025-09-03*  
*Version: 1.0*  
*Established for Production v0.3.0*