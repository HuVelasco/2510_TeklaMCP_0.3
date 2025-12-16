#!/usr/bin/env python3
"""Apply MODEL object bounding box fix to extraction template"""

# Read the file
with open('Server/TeklaMCP/ProgramClean.cs', 'r', encoding='utf-8') as f:
    lines = f.readlines()

# Find and replace lines 1663-1703 (the bounding box section)
# Look for the start marker
start_line = None
end_line = None
for i, line in enumerate(lines):
    if '// ENHANCEMENT: Extract bounding box (AABB) data inspired by tekladrawingexamples.cs' in line:
        start_line = i
    if start_line is not None and 'catch { /* Skip bounding box if extraction fails */ }' in line:
        end_line = i
        break

if start_line is None or end_line is None:
    print(f"ERROR: Could not find bounding box section (start={start_line}, end={end_line})")
    exit(1)

print(f"Found bounding box section: lines {start_line+1} to {end_line+1}")

# New code (with proper indentation and escaped quotes for template string)
new_code = [
    '                    // ENHANCEMENT: Extract bounding box from MODEL Part solid (tekladrawingexamples.cs:2059)\n',
    '                    try\n',
    '                    {\n',
    '                        var drawingPart = obj as Tekla.Structures.Drawing.Part;\n',
    '                        if (drawingPart != null && drawingPart.ModelIdentifier != null)\n',
    '                        {',
    '\n',
    '                            var model = new Tekla.Structures.Model.Model();\n',
    '                            var modelObj = model.SelectModelObject(drawingPart.ModelIdentifier);\n',
    '                            var modelPart = modelObj as Tekla.Structures.Model.Part;\n',
    '                            if (modelPart != null)\n',
    '                            {\n',
    '                                var modelSolid = modelPart.GetSolid();\n',
    '                                if (modelSolid != null)\n',
    '                                {\n',
    '                                    var minPt = modelSolid.MinimumPoint;\n',
    '                                    var maxPt = modelSolid.MaximumPoint;\n',
    '                                    partGeometry[""bounding_box_model""] = new {\n',
    '                                        min = new { x = minPt.X, y = minPt.Y, z = minPt.Z },\n',
    '                                        max = new { x = maxPt.X, y = maxPt.Y, z = maxPt.Z },\n',
    '                                        type = ""model_global_coordinates""\n',
    '                                    };\n',
    '                                }\n',
    '                            }\n',
    '                        }\n',
    '                    }\n',
    '                    catch (Exception ex) { partGeometry[""bbox_error""] = ex.Message; }\n',
]

# Replace the section
new_lines = lines[:start_line] + new_code + lines[end_line+1:]

# Write back
with open('Server/TeklaMCP/ProgramClean.cs', 'w', encoding='utf-8') as f:
    f.writelines(new_lines)

print(f"Applied fix: replaced {end_line - start_line + 1} lines with {len(new_code)} lines")
print("Bounding box extraction now uses MODEL object")
