#!/usr/bin/env python3
"""
Apply extraction enhancement fixes to ProgramClean.cs

This script applies the two critical fixes documented in EXTRACTION_FIXES.md:
1. Bounding box extraction using drawing interfaces
2. Dimension position extraction using bounding boxes
"""

import re

# Read the file
with open('Server/TeklaMCP/ProgramClean.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Fix 1: Replace bounding box extraction code (lines ~1659-1700)
old_bbox_pattern = r'// ENHANCEMENT: Extract bounding box \(AABB\) data inspired by tekladrawingexamples\.cs.*?catch \{ /\* Skip bounding box if extraction fails \*/ \}'

new_bbox_code = '''// ENHANCEMENT: Extract bounding box using drawing interfaces (tekladrawingexamples.cs:2148-2171)
                            // FIX: Use IAxisAlignedBoundingBox and IObjectAlignedBoundingBox interfaces
                            try
                            {
                                // Check if this Part implements IAxisAlignedBoundingBox
                                if (obj is Tekla.Structures.Drawing.IAxisAlignedBoundingBox)
                                {
                                    var aabbInterface = obj as Tekla.Structures.Drawing.IAxisAlignedBoundingBox;
                                    var bbox = aabbInterface.GetAxisAlignedBoundingBox();
                                    if (bbox != null)
                                    {
                                        partGeometry[""bounding_box_aabb""] = new {
                                            lower_left = new { x = bbox.LowerLeft.X, y = bbox.LowerLeft.Y, z = bbox.LowerLeft.Z },
                                            upper_right = new { x = bbox.UpperRight.X, y = bbox.UpperRight.Y, z = bbox.UpperRight.Z },
                                            lower_right = new { x = bbox.LowerRight.X, y = bbox.LowerRight.Y, z = bbox.LowerRight.Z },
                                            upper_left = new { x = bbox.UpperLeft.X, y = bbox.UpperLeft.Y, z = bbox.UpperLeft.Z },
                                            type = ""axis_aligned""
                                        };
                                    }
                                }

                                // Also extract Object-Aligned Bounding Box for rotated parts
                                if (obj is Tekla.Structures.Drawing.IObjectAlignedBoundingBox)
                                {
                                    var oabbInterface = obj as Tekla.Structures.Drawing.IObjectAlignedBoundingBox;
                                    var obbox = oabbInterface.GetObjectAlignedBoundingBox();
                                    if (obbox != null)
                                    {
                                        partGeometry[""bounding_box_oabb""] = new {
                                            lower_left = new { x = obbox.LowerLeft.X, y = obbox.LowerLeft.Y, z = obbox.LowerLeft.Z },
                                            upper_right = new { x = obbox.UpperRight.X, y = obbox.UpperRight.Y, z = obbox.UpperRight.Z },
                                            lower_right = new { x = obbox.LowerRight.X, y = obbox.LowerRight.Y, z = obbox.LowerRight.Z },
                                            upper_left = new { x = obbox.UpperLeft.X, y = obbox.UpperLeft.Y, z = obbox.UpperLeft.Z },
                                            angle_to_axis = obbox.AngleToAxis,
                                            type = ""object_aligned""
                                        };
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                LogMessage($""BBOX_EXTRACTION_ERROR: {ex.Message}"");
                            }'''

content = re.sub(old_bbox_pattern, new_bbox_code, content, flags=re.DOTALL)

# Fix 2: Replace dimension position extraction code (lines ~1526-1551)
old_dim_pattern = r'// ENHANCEMENT: Extract InsertionPoint for dimension text placement.*?catch \{ /\* Skip if InsertionPoint not available \*/ \}'

new_dim_code = '''// ENHANCEMENT: Extract dimension placement data (ML training feature)
                    // FIX: Dimensions don't have InsertionPoint - use bounding box for text position
                    try
                    {
                        // Extract dimension bounding box (text frame position)
                        if (obj is Tekla.Structures.Drawing.IObjectAlignedBoundingBox)
                        {
                            var dimBBox = (obj as Tekla.Structures.Drawing.IObjectAlignedBoundingBox).GetObjectAlignedBoundingBox();
                            if (dimBBox != null)
                            {
                                // The dimension text is positioned within this bounding box
                                geometricData[""dimension_text_frame""] = new {
                                    lower_left = new { x = dimBBox.LowerLeft.X, y = dimBBox.LowerLeft.Y },
                                    upper_right = new { x = dimBBox.UpperRight.X, y = dimBBox.UpperRight.Y },
                                    center = new {
                                        x = (dimBBox.LowerLeft.X + dimBBox.UpperRight.X) / 2,
                                        y = (dimBBox.LowerLeft.Y + dimBBox.UpperRight.Y) / 2
                                    },
                                    angle = dimBBox.AngleToAxis,
                                    type = ""dimension_text_bounding_box""
                                };
                            }
                        }
                    }
                    catch { /* Skip if dimension bounding box not available */ }'''

content = re.sub(old_dim_pattern, new_dim_code, content, flags=re.DOTALL)

# Write the fixed content
with open('Server/TeklaMCP/ProgramClean.cs', 'w', encoding='utf-8') as f:
    f.write(content)

print(" Applied extraction fixes to ProgramClean.cs")
print("   - Fixed bounding box extraction using drawing interfaces")
print("   - Fixed dimension position extraction using bounding boxes")
print("\nNext steps:")
print("1. Build: cd Server/TeklaMCP && dotnet build")
print("2. Test: Run extraction with 2 drawings")
print("3. Verify: python verify_enhancements.py <extraction_file>")
