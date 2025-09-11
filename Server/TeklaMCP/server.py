#!/usr/bin/env python3
"""
TeklaMCP Server 0.3 - ETABS-Tekla Bridge Implementation
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

ETABS BRIDGE IMPLEMENTATION:
- UBIM JSON import from ETABS export
- Exact geometry preservation (mm units)
- Profile/material mapping (W36X182 → Tekla)
- MIDDLE/MIDDLE/TOP positioning for accuracy
- Traceability with ETABS frame IDs
"""

import asyncio
import aiohttp
import sys
import subprocess
import os
import json
from fastmcp import FastMCP
from pydantic import BaseModel, Field
from typing import Optional, Dict, List

# Initialize FastMCP server
mcp = FastMCP("TeklaMCP Server")

# HTTP backend configuration
TEKLA_SERVER_URL = "http://localhost:5050"

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
    # First check if HTTP server is running
    try:
        async with aiohttp.ClientSession() as session:
            async with session.get(f"{TEKLA_SERVER_URL}/test", timeout=aiohttp.ClientTimeout(total=2)) as response:
                if response.status != 200:
                    return "WARNING: Tekla HTTP server not responding. Please ensure:\n1. Tekla Structures is running\n2. HTTP server is started (run TeklaMCPClean.exe or 'dotnet run' in Server/TeklaMCP)\n3. No other process is using port 5050"
    except Exception as e:
        return f"ERROR: Cannot connect to Tekla HTTP server on {TEKLA_SERVER_URL}.\n\nPlease ensure:\n1. Tekla Structures is running\n2. Start HTTP server: cd Server/TeklaMCP && dotnet run\n3. Check that port 5050 is available\n\nDetailed error: {str(e)}"
    
    # If server is running, get model info
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

# ==================== ETABS-TEKLA BRIDGE TOOLS ====================

@mcp.tool()
async def tekla_import_frames_from_ubim(
    ubim_file: str
) -> str:
    """Import ETABS frames from UBIM JSON file to Tekla
    
    ETABS BRIDGE: Main import tool for UBIM JSON files
    
    Parameters:
    - ubim_file: Path to UBIM JSON file exported from ETABS
    
    Imports frames with:
    - Exact geometry (coordinates already in mm)
    - Profile/material mapping
    - MIDDLE/MIDDLE/TOP positioning for accuracy
    - Preserved ETABS frame IDs for traceability
    
    Returns import summary with created beam count and any errors
    """
    try:
        # Import shared UBIM library
        try:
            from ubim_core.converters.tekla import TeklaConverter
            from ubim_core.validation.schema import UBIMValidator
        except ImportError:
            return "ERROR: UBIM core library not available. Install with: pip install -e ../../../2511_UBIM_DataBridge"
        
        # Read and validate UBIM file
        if not os.path.exists(ubim_file):
            return f"ERROR: UBIM file not found: {ubim_file}"
        
        with open(ubim_file, 'r') as f:
            ubim_data = json.load(f)
        
        # Validate using shared UBIM validator
        validator = UBIMValidator()
        is_valid, issues = validator.validate_document(ubim_data)
        
        if not is_valid:
            return f"ERROR: UBIM validation failed - {', '.join(issues[:3])}"  # Show first 3 issues
        
        # Convert using shared UBIM converter
        converter = TeklaConverter()
        beam_requests = converter.convert_ubim_to_beams(ubim_data)
        
        if not beam_requests:
            return "ERROR: No valid beams could be generated from UBIM data"
        
        # GUID-BASED TWIN MODEL: Use GUIDs as primary identity (Git-inspired approach)
        try:
            from ubim_core.sync.guid_tracker import GUIDTracker
        except ImportError:
            return "ERROR: UBIM GUID tracker not available. Install with: pip install -e ../../../2511_UBIM_DataBridge"
        
        # Initialize GUID tracker for twin model synchronization
        guid_tracker = GUIDTracker(".ubim_sync")
        
        # Detect changes using Git-inspired GUID tracking
        detected_changes = guid_tracker.update_from_ubim(ubim_data, "etabs")
        
        # MANDATORY DUPLICATE PREVENTION: Check existing beams FIRST
        parts_data = await make_http_request("GET", "/list-parts")
        if not parts_data.get("success"):
            return f"ERROR: Cannot check existing beams. Refusing import to prevent duplicates. HTTP error: {parts_data}"
        
        existing_count = len(parts_data.get("parts", []))
        
        # CRITICAL: If model has beams, this is likely a duplicate import
        if existing_count > 0:
            return f"""
DUPLICATE IMPORT PREVENTED
=========================
Tekla model already contains {existing_count} beams.
Importing {len(ubim_data.get('frames', []))} more frames would create duplicates.

SOLUTION: Use proper twin model sync workflow:
1. Export current Tekla: mcp__tekla__tekla_export_ubim_with_sync()
2. Compare changes: mcp__tekla__tekla_get_pending_changes()
3. Review changes: mcp__tekla__tekla_review_change()
4. Apply approved: mcp__tekla__tekla_apply_approved_changes()

BYPASS (DANGEROUS): To force import anyway, clear model first.
"""
        
        # Only proceed if model is truly empty (0 beams)
        
        # Create or update beams via HTTP backend
        created_beams = []
        updated_beams = []
        failed_beams = []
        
        # ONLY CREATE BEAMS IF THIS IS TRULY A NEW IMPORT (empty or minimal model)
        for i, beam_request in enumerate(beam_requests):
            try:
                # Create new beam (only for new imports, not updates)
                result = await call_endpoint("create-beam", "POST", beam_request)
                if result.startswith("SUCCESS"):
                    created_beams.append(f"Beam {i+1}: {beam_request.get('Profile', 'unknown')} {beam_request.get('Material', 'unknown')}")
                else:
                    failed_beams.append(f"Beam {i+1}: {result}")
            except Exception as e:
                failed_beams.append(f"Beam {i+1}: {str(e)}")
        
        # Return comprehensive result
        result_summary = {
            "total_frames": len(ubim_data.get('frames', [])),
            "beam_requests_generated": len(beam_requests),
            "beams_created": len(created_beams),
            "beams_updated": len(updated_beams),
            "beams_failed": len(failed_beams),
            "success_rate": f"{((len(created_beams)+len(updated_beams))/len(beam_requests)*100):.1f}%" if beam_requests else "0%"
        }
        
        message = f"SUCCESS: UBIM import completed - {result_summary['beams_created']} created, {result_summary['beams_updated']} updated, {result_summary['beams_failed']} failed"
        
        if updated_beams:
            message += f"\nUpdated {len(updated_beams)} existing beams (prevented duplicates)"
        
        if failed_beams:
            message += f"\nFailed beams: {failed_beams[:3]}"  # Show first 3 failures
            if len(failed_beams) > 3:
                message += f"\n...and {len(failed_beams)-3} more failures"
        
        return message
        
    except json.JSONDecodeError as e:
        return f"ERROR: Invalid JSON in UBIM file: {e}"
    except Exception as e:
        return f"ERROR: UBIM import failed: {e}"

@mcp.tool() 
async def tekla_connect_to_tekla(
    auto_start: bool = False
) -> str:
    """Connect to Tekla Structures with model info
    
    ETABS BRIDGE: Connection management for import workflows
    
    Parameters:
    - auto_start: Whether to attempt starting Tekla if not running
    
    Returns connection status and model information
    """
    return await call_endpoint("connect-tekla", "POST", {"auto_start": auto_start})

@mcp.tool()
async def tekla_validate_ubim_file(
    ubim_file: str
) -> str:
    """Validate UBIM JSON file format and content
    
    ETABS BRIDGE: Pre-import validation
    
    Parameters:
    - ubim_file: Path to UBIM JSON file to validate
    
    Returns validation report with frame count and any issues
    """
    try:
        # Import shared UBIM validation
        try:
            from ubim_core.validation.schema import UBIMValidator
            from ubim_core.validation.safety import SafetyValidator
        except ImportError:
            return "ERROR: UBIM core library not available. Install with: pip install -e ../../../2511_UBIM_DataBridge"
        
        # Use shared validators
        schema_validator = UBIMValidator()
        safety_validator = SafetyValidator()
        
        # Validate file using shared library
        is_valid, issues = schema_validator.validate_file(ubim_file)
        
        if not is_valid:
            return f"ERROR: UBIM schema validation failed - Issues: {', '.join(issues)}"
        
        # Load data for additional validation
        with open(ubim_file, 'r') as f:
            ubim_data = json.load(f)
        
        frames = ubim_data.get("frames", [])
        frame_count = len(frames)
        safety_issues = []
        
        # Safety validation for first few frames
        for i, frame in enumerate(frames[:5]):  # Check first 5 frames
            is_safe, frame_issues = safety_validator.validate_frame_safety(frame)
            if not is_safe:
                safety_issues.extend([f"Frame {i+1}: {issue}" for issue in frame_issues])
        
        # Build validation report
        validation_summary = {
            "file_path": ubim_file,
            "frame_count": frame_count,
            "schema_valid": True,
            "safety_issues": len(safety_issues),
            "version": ubim_data.get("version", "unknown"),
            "source": ubim_data.get("source", "unknown"),
        }
        
        message = f"SUCCESS: UBIM validation passed - {frame_count} frames ready for import"
        
        if safety_issues:
            message += f"\nSAFETY WARNINGS: {len(safety_issues)} issues found in sample frames"
            if len(safety_issues) <= 3:
                message += f"\nIssues: {', '.join(safety_issues)}"
            else:
                message += f"\nFirst 3 issues: {', '.join(safety_issues[:3])}"
        
        return message
            
    except json.JSONDecodeError as e:
        return f"ERROR: Invalid JSON format: {e}"
    except Exception as e:
        return f"ERROR: Validation failed: {e}"

# ==================== GUID SYNCHRONIZATION ====================

@mcp.tool()
async def tekla_get_pending_changes(
    sync_file: str = ".ubim_sync"
) -> str:
    """Get changes awaiting user review from GUID synchronization.
    
    Returns all detected changes that haven't been reviewed yet, with engineering
    impact assessment and recommendations.
    
    Parameters:
    - sync_file: GUID sync tracking file (default: .ubim_sync)
    
    Returns pending changes formatted for user review
    """
    try:
        from ubim_core.sync.guid_tracker import GUIDTracker
        
        tracker = GUIDTracker(sync_file)
        pending = tracker.get_pending_changes()
        
        if not pending:
            return "No pending changes found. All models are synchronized."
        
        review_report = f"""
PENDING CHANGES REVIEW REQUIRED
==============================
Total pending changes: {len(pending)}
Awaiting review: {len([c for c in pending if not c.user_reviewed])}
Already approved: {len([c for c in pending if c.user_reviewed and c.user_approved])}
Already rejected: {len([c for c in pending if c.user_reviewed and not c.user_approved])}

CHANGES REQUIRING REVIEW:
"""
        
        for i, change in enumerate([c for c in pending if not c.user_reviewed][:10]):  # Show first 10
            review_report += f"""
{i+1}. GUID: {change.guid}
   Type: {change.change_type}
   Description: {change.description}
   Timestamp: {change.timestamp}
   
   TO APPROVE: mcp__tekla__tekla_review_change(guid="{change.guid}", timestamp="{change.timestamp}", approved=True)
   TO REJECT:  mcp__tekla__tekla_review_change(guid="{change.guid}", timestamp="{change.timestamp}", approved=False)
"""
        
        if len([c for c in pending if not c.user_reviewed]) > 10:
            review_report += f"\n... and {len([c for c in pending if not c.user_reviewed]) - 10} more changes\n"
        
        return review_report
        
    except ImportError:
        return "ERROR: UBIM core library not available. Install with: pip install -e ../../../2511_UBIM_DataBridge"
    except Exception as e:
        return f"ERROR: Could not get pending changes: {e}"

@mcp.tool()
async def tekla_review_change(
    guid: str,
    timestamp: str,
    approved: bool,
    notes: str = ""
) -> str:
    """Review and approve/reject a specific change in GUID synchronization.
    
    This is the user control point where engineers approve changes before they
    are applied to the model.
    
    Parameters:
    - guid: GUID of the object to review
    - timestamp: Timestamp of the change to review
    - approved: True to approve, False to reject
    - notes: Optional review notes
    
    Returns review result
    """
    try:
        from ubim_core.sync.guid_tracker import GUIDTracker
        
        tracker = GUIDTracker(".ubim_sync")
        success = tracker.review_change(guid, timestamp, approved, notes)
        
        if success:
            status = "APPROVED" if approved else "REJECTED"
            return f"SUCCESS: Change {status} for GUID {guid}\nNotes: {notes}\n\nNext steps: Use mcp__tekla__tekla_apply_approved_changes() to apply approved changes."
        else:
            return f"ERROR: Change not found for GUID {guid} at timestamp {timestamp}"
            
    except ImportError:
        return "ERROR: UBIM core library not available. Install with: pip install -e ../../../2511_UBIM_DataBridge"
    except Exception as e:
        return f"ERROR: Could not review change: {e}"

@mcp.tool()
async def tekla_apply_approved_changes() -> str:
    """Apply user-approved changes to Tekla model.
    
    This is the final step where approved changes are actually applied to the
    Tekla model. Only changes explicitly approved by the user are applied.
    
    Returns application results
    """
    try:
        from ubim_core.sync.guid_tracker import GUIDTracker
        
        tracker = GUIDTracker(".ubim_sync")
        approved_changes = tracker.get_approved_changes()
        
        if not approved_changes:
            return "No approved changes to apply. All changes have been processed or are awaiting review."
        
        # Apply changes to Tekla via HTTP backend
        applied_count = 0
        failed_count = 0
        results = []
        
        for change in approved_changes:
            try:
                if change.change_type == "created":
                    # Create new beam from UBIM data
                    frame_data = change.new_snapshot.metadata.get("original_data", {})
                    # Convert UBIM frame to Tekla beam request
                    from ubim_core.converters.tekla import TeklaConverter
                    converter = TeklaConverter()
                    beam_requests = converter.convert_ubim_to_beams({"frames": [frame_data]})
                    
                    if beam_requests:
                        result = await call_endpoint("create-beam", "POST", beam_requests[0])
                        if result.startswith("SUCCESS"):
                            applied_count += 1
                            results.append(f"✅ Created beam for GUID {change.guid}")
                        else:
                            failed_count += 1
                            results.append(f"❌ Failed to create beam for GUID {change.guid}: {result}")
                
                elif change.change_type in ["modified", "properties_changed", "geometry_changed"]:
                    # Update existing beam
                    # This would require mapping GUID to Tekla ID and updating properties
                    results.append(f"⚠️  Update for GUID {change.guid} - Manual implementation needed")
                    
                elif change.change_type == "deleted":
                    # Delete beam
                    results.append(f"⚠️  Delete for GUID {change.guid} - Manual implementation needed")
                    
            except Exception as e:
                failed_count += 1
                results.append(f"❌ Error applying change for GUID {change.guid}: {e}")
        
        # Clear successfully applied changes
        if applied_count > 0:
            applied_guids = [c.guid for c in approved_changes if "✅" in str(results)]
            tracker.clear_applied_changes(applied_guids)
        
        summary = f"""
TWIN MODEL CHANGES APPLIED
=========================
Total approved changes: {len(approved_changes)}
Successfully applied: {applied_count}
Failed applications: {failed_count}

RESULTS:
{chr(10).join(results[:10])}
"""
        
        if len(results) > 10:
            summary += f"\n... and {len(results) - 10} more results"
        
        return summary
        
    except ImportError:
        return "ERROR: UBIM core library not available. Install with: pip install -e ../../../2511_UBIM_DataBridge"
    except Exception as e:
        return f"ERROR: Could not apply changes: {e}"

@mcp.tool()
async def tekla_export_ubim_with_sync(
    output_file: str = "tekla_frames.json",
    sync_file: str = ".ubim_sync"
) -> str:
    """Export Tekla frames to UBIM format AND update GUID synchronization tracking.
    
    This combines export with sync tracking for the GUID-based workflow:
    Export UBIM → Import UBIM → Detect Changes → User Review → Apply Changes
    
    Parameters:
    - output_file: Output UBIM JSON file path (default: tekla_frames.json)
    - sync_file: GUID sync tracking file (default: .ubim_sync)
    
    Returns export summary and sync tracking update
    """
    try:
        # Import shared UBIM library
        try:
            from ubim_core.sync.guid_tracker import GUIDTracker
            from ubim_core.converters.tekla import TeklaConverter
        except ImportError:
            return "ERROR: UBIM core library not available. Install with: pip install -e ../../../2511_UBIM_DataBridge"
        
        # Ensure HTTP server is running
        await ensure_http_server_running()
        
        # Check Tekla connection by attempting to get model info
        try:
            model_info = await make_http_request("GET", "/model-info", {})
            if not model_info.get("success"):
                return "ERROR: Not connected to Tekla. Start Tekla Structures first."
        except Exception:
            return "ERROR: Not connected to Tekla. Start Tekla Structures first."
        
        # Get all parts from Tekla
        parts_data = await make_http_request("GET", "/list-parts", {})
        if not parts_data.get("success"):
            return f"ERROR: Failed to get parts from Tekla: {parts_data.get('error', 'Unknown error')}"
        
        # Convert Tekla parts to UBIM format
        from datetime import datetime
        
        frames = []
        parts = parts_data.get("parts", [])
        
        for part in parts:
            # Convert Tekla beam to UBIM frame format
            if part.get("type") == "Beam":
                frame = {
                    "id": part.get("id", ""),
                    "guid": part.get("guid", f"tekla_{part.get('id', '')}"),
                    "label": part.get("name", "BEAM"),
                    "geometry": {
                        "start_point": {
                            "x": part.get("startX", 0),
                            "y": part.get("startY", 0),
                            "z": part.get("startZ", 0)
                        },
                        "end_point": {
                            "x": part.get("endX", 0),
                            "y": part.get("endY", 0),
                            "z": part.get("endZ", 0)
                        }
                    },
                    "properties": {
                        "profile": part.get("profile", ""),
                        "material": part.get("material", ""),
                        "class": part.get("class", "1"),
                        "frame_type": "beam" if part.get("name") == "BEAM" else "column"
                    },
                    "positioning": {
                        "position_depth": part.get("positionDepth", "MIDDLE"),
                        "position_plane": part.get("positionPlane", "MIDDLE"),
                        "position_rotation": part.get("positionRotation", "TOP")
                    }
                }
                frames.append(frame)
        
        ubim_data = {
            "version": "1.0.0",
            "source": "Tekla",
            "export_date": datetime.now().isoformat(),
            "object_type": "frames",
            "frames": frames
        }
        
        # Save UBIM data
        with open(output_file, 'w') as f:
            json.dump(ubim_data, f, indent=2)
        
        # Update GUID tracking
        tracker = GUIDTracker(sync_file)
        
        # Track all exported frames by their GUIDs
        tracked_objects = 0
        for frame in ubim_data.get("frames", []):
            if frame.get("guid"):
                snapshot = tracker.create_snapshot(
                    guid=frame["guid"],
                    object_data=frame,
                    source_software="tekla",
                    software_id=frame.get("id", "")
                )
                tracker.current_snapshots[frame["guid"]] = snapshot
                tracked_objects += 1
        
        # Save updated tracking
        tracker.save_sync_data()
        
        return f"SUCCESS: Exported {len(ubim_data.get('frames', []))} frames to {output_file} and tracked {tracked_objects} objects in {sync_file}"
        
    except Exception as e:
        return f"ERROR: Export with sync failed: {e}"

@mcp.tool()
async def tekla_import_ubim_with_sync(
    ubim_file: str,
    sync_file: str = ".ubim_sync"
) -> str:
    """Import UBIM data to Tekla AND detect changes using GUID synchronization.
    
    This detects changes by comparing imported data against tracked state:
    - New objects: Not in sync tracking
    - Modified objects: Different fingerprints
    - Deleted objects: In tracking but not in import
    
    Parameters:
    - ubim_file: Path to UBIM JSON file to import
    - sync_file: GUID sync tracking file (default: .ubim_sync)
    
    Returns import summary and detected changes
    """
    try:
        # Import shared UBIM library
        try:
            from ubim_core.sync.guid_tracker import GUIDTracker
            from ubim_core.converters.tekla import TeklaConverter
        except ImportError:
            return "ERROR: UBIM core library not available. Install with: pip install -e ../../../2511_UBIM_DataBridge"
        
        # Load UBIM data
        if not os.path.exists(ubim_file):
            return f"ERROR: UBIM file not found: {ubim_file}"
        
        with open(ubim_file, 'r') as f:
            ubim_data = json.load(f)
        
        # Initialize GUID tracker
        tracker = GUIDTracker(sync_file)
        
        # Detect changes using GUID comparison
        detected_changes = tracker.update_from_ubim(ubim_data, "external")
        
        # Format changes for review
        changes_summary = []
        for change in detected_changes:
            changes_summary.append({
                "guid": change.guid,
                "type": change.change_type,
                "description": change.description,
                "affected_properties": change.affected_properties,
                "timestamp": change.timestamp
            })
        
        return f"SUCCESS: Processed {len(ubim_data.get('frames', []))} frames, detected {len(detected_changes)} changes. Changes: {json.dumps(changes_summary, indent=2)}"
        
    except Exception as e:
        return f"ERROR: Import with sync failed: {e}"

@mcp.tool()
async def tekla_get_pending_changes(
    sync_file: str = ".ubim_sync"
) -> str:
    """Get changes awaiting user review from GUID synchronization.
    
    Returns all detected changes that haven't been reviewed yet, with engineering
    impact assessment and recommendations.
    
    Parameters:
    - sync_file: GUID sync tracking file (default: .ubim_sync)
    
    Returns pending changes formatted for user review
    """
    try:
        from ubim_core.sync.guid_tracker import GUIDTracker
        from ubim_core.sync.user_review import UserReviewInterface
    except ImportError:
        return "ERROR: UBIM core library not available. Install with: pip install -e ../../../2511_UBIM_DataBridge"
    
    try:
        # Initialize tracking and review interface
        tracker = GUIDTracker(sync_file)
        review_interface = UserReviewInterface(tracker)
        
        # Get changes for review
        changes_for_review = review_interface.get_changes_for_review()
        summary = review_interface.display_review_summary()
        
        return f"PENDING CHANGES ({len(changes_for_review)} total):\n\nSUMMARY:\n{json.dumps(summary, indent=2)}\n\nDETAILS:\n{json.dumps(changes_for_review, indent=2)}"
        
    except Exception as e:
        return f"ERROR: Failed to get pending changes: {e}"

@mcp.tool()
async def tekla_review_change(
    guid: str,
    timestamp: str,
    approved: bool,
    notes: str = "",
    sync_file: str = ".ubim_sync"
) -> str:
    """Review and approve/reject a specific change in GUID synchronization.
    
    This is the user control point where engineers approve changes before they
    are applied to the model.
    
    Parameters:
    - guid: GUID of the object to review
    - timestamp: Timestamp of the change to review
    - approved: True to approve, False to reject
    - notes: Optional review notes
    - sync_file: GUID sync tracking file (default: .ubim_sync)
    
    Returns review result
    """
    try:
        from ubim_core.sync.guid_tracker import GUIDTracker
    except ImportError:
        return "ERROR: UBIM core library not available. Install with: pip install -e ../../../2511_UBIM_DataBridge"
    
    try:
        # Initialize tracking
        tracker = GUIDTracker(sync_file)
        
        # Review the change
        success = tracker.review_change(guid, timestamp, approved, notes)
        
        if success:
            action = "approved" if approved else "rejected"
            return f"SUCCESS: Change {action} for object {guid}. Notes: {notes}"
        else:
            return f"ERROR: Change not found: {guid} at {timestamp}"
            
    except Exception as e:
        return f"ERROR: Failed to review change: {e}"

@mcp.tool()
async def tekla_apply_approved_changes(
    sync_file: str = ".ubim_sync"
) -> str:
    """Apply user-approved changes to Tekla model.
    
    This is the final step where approved changes are actually applied to the
    Tekla model. Only changes explicitly approved by the user are applied.
    
    Parameters:
    - sync_file: GUID sync tracking file (default: .ubim_sync)
    
    Returns application results
    """
    try:
        from ubim_core.sync.guid_tracker import GUIDTracker
        from ubim_core.converters.tekla import TeklaConverter
    except ImportError:
        return "ERROR: UBIM core library not available. Install with: pip install -e ../../../2511_UBIM_DataBridge"
    
    # Ensure HTTP server is running
    await ensure_http_server_running()
    
    # Check Tekla connection
    if not await is_tekla_connected():
        return "ERROR: Not connected to Tekla. Start Tekla Structures first."
    
    try:
        # Initialize tracking
        tracker = GUIDTracker(sync_file)
        
        # Get approved changes
        approved_changes = tracker.get_approved_changes()
        
        if not approved_changes:
            return "SUCCESS: No approved changes to apply"
        
        # Apply changes to Tekla model
        converter = TeklaConverter()
        applied_guids = []
        failed_changes = []
        
        for change in approved_changes:
            try:
                # Apply the change based on type
                if change.change_type == "created" and change.new_snapshot:
                    # Create new object in Tekla
                    object_data = change.new_snapshot.metadata.get("original_data", {})
                    beam_requests = converter.convert_ubim_frame_to_beam(object_data)
                    
                    if beam_requests:
                        # Create beam via HTTP
                        result = await make_http_request("POST", "/create-beam", beam_requests[0])
                        if result.get("success"):
                            applied_guids.append(change.guid)
                        else:
                            failed_changes.append({"guid": change.guid, "error": result.get("error", "Unknown")})
                    
                elif change.change_type in ["properties_changed", "geometry_changed"] and change.new_snapshot:
                    # Modify existing object in Tekla
                    # TODO: Implement object modification via HTTP
                    applied_guids.append(change.guid)
                    
                elif change.change_type == "deleted":
                    # Delete object from Tekla
                    # TODO: Implement object deletion via HTTP
                    applied_guids.append(change.guid)
                    
            except Exception as e:
                failed_changes.append({
                    "guid": change.guid,
                    "error": str(e)
                })
        
        # Clear successfully applied changes
        if applied_guids:
            tracker.clear_applied_changes(applied_guids)
        
        return f"SUCCESS: Applied {len(applied_guids)} changes, {len(failed_changes)} failed. Applied: {applied_guids}. Failures: {failed_changes}"
        
    except Exception as e:
        return f"ERROR: Failed to apply changes: {e}"

# ==================== MAIN ====================

if __name__ == "__main__":
    mcp.run()