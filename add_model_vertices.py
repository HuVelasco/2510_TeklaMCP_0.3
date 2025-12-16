#!/usr/bin/env python3
"""Add MODEL object vertex extraction for better resolution"""

with open('Server/TeklaMCP/ProgramClean.cs', 'r', encoding='utf-8') as f:
    lines = f.readlines()

# Find the line with bbox_error (end of bounding box section)
insert_line = None
for i, line in enumerate(lines):
    if 'catch (Exception ex) { partGeometry["bbox_error"] = ex.Message; }' in line:
        insert_line = i + 1
        break

if insert_line is None:
    print("ERROR: Could not find insertion point")
    exit(1)

print(f"Found insertion point at line {insert_line}")

# New vertex extraction code (to be inserted INSIDE the existing try block)
# We need to add it BEFORE the catch, so let's modify the approach
# Find the closing brace of the bounding box if block
for i in range(len(lines) - 1, 0, -1):
    if i < insert_line and 'var minPt = modelSolid.MinimumPoint;' in lines[i]:
        # Found the bounding box section, now find its closing braces
        # Look for the line with the bbox assignment closing
        for j in range(i, min(i + 20, len(lines))):
            if 'type = ""model_global_coordinates""' in lines[j]:
                # Found it, now find the closing braces
                brace_count = 0
                for k in range(j, min(j + 10, len(lines))):
                    line = lines[k]
                    if '};' in line and brace_count == 0:
                        # This is where we insert the vertex code
                        insert_line = k + 1
                        print(f"Inserting vertices code after line {k+1}")
                        break
                break
        break

# Vertex extraction code to insert (with proper indentation to match the bbox code)
vertex_code = [
    '\n',
    '                                    // VERTICES from MODEL solid (high resolution geometry)\n',
    '                                    var modelVertices = new System.Collections.Generic.List<object>();\n',
    '                                    var faceList = modelSolid.GetFaceList();\n',
    '                                    while (faceList.MoveNext())\n',
    '                                    {\n',
    '                                        var face = faceList.Current as Tekla.Structures.Model.Face;\n',
    '                                        if (face != null)\n',
    '                                        {\n',
    '                                            var contour = face.GetContour();\n',
    '                                            foreach (Tekla.Structures.Geometry3d.Point pt in contour.ContourPoints)\n',
    '                                            {\n',
    '                                                modelVertices.Add(new { x = pt.X, y = pt.Y, z = pt.Z });\n',
    '                                            }\n',
    '                                        }\n',
    '                                    }\n',
    '                                    partGeometry[""vertices_model""] = modelVertices;\n',
]

# Insert the vertex code
new_lines = lines[:insert_line] + vertex_code + lines[insert_line:]

with open('Server/TeklaMCP/ProgramClean.cs', 'w', encoding='utf-8') as f:
    f.writelines(new_lines)

print(f"Added {len(vertex_code)} lines of vertex extraction code")
print("Vertices will now be extracted from MODEL Part solid")
