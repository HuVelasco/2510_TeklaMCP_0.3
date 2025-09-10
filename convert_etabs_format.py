#!/usr/bin/env python3
"""
Convert ETABS export from old format to new format for TeklaMCP 0.3
Converts orientation/offsets structure to design_orientation/local_angle structure
"""

import json
import sys

def convert_frame_format(old_frame):
    """Convert a single frame from old format to new format"""
    
    # Extract local rotation angle from orientation data
    local_angle = 0.0
    if "orientation" in old_frame:
        orientation = old_frame["orientation"]
        # Use rotation_z for local angle (most common for steel frame rotation)
        # If rotation_z is 0, check rotation_x (for rotated columns)
        if orientation.get("rotation_z", 0.0) != 0.0:
            local_angle = orientation["rotation_z"]
        elif orientation.get("rotation_x", 0.0) != 0.0:
            local_angle = orientation["rotation_x"]
        elif orientation.get("rotation_y", 0.0) != 0.0:
            local_angle = orientation["rotation_y"]
    
    new_frame = {
        "id": old_frame["id"],
        "type": old_frame["type"],  # Keep original type
        "design_orientation": old_frame["type"],  # Use type as design_orientation
        "local_angle": local_angle,  # Extract actual rotation angle
        "geometry": old_frame["geometry"],
        "properties": {
            "profile": old_frame["properties"]["profile"],
            "material": old_frame["properties"]["material"], 
            "story": old_frame["properties"]["story"],
            "cardinal_point": old_frame.get("orientation", {}).get("cardinal_point", 5),  # Default to center
            "frame_class": ""  # Empty frame_class
        }
    }
    
    # Add GUID if available
    if "guid" in old_frame:
        new_frame["guid"] = old_frame["guid"]
    else:
        # Generate a simple GUID-like string from ID
        new_frame["guid"] = f"etabs-{old_frame['id']}-converted"
    
    return new_frame

def convert_etabs_export(input_file, output_file):
    """Convert entire ETABS export file"""
    try:
        with open(input_file, 'r') as f:
            old_data = json.load(f)
        
        print(f"Loading {input_file}...")
        print(f"Found {len(old_data.get('frames', []))} frames")
        
        # Convert to new format
        new_data = {
            "version": "1.0.0",
            "source": "ETABS",
            "units": old_data.get("units", {
                "length": "mm",
                "force": "N", 
                "angle": "degrees"
            }),
            "frames": []
        }
        
        # Convert each frame
        converted_count = 0
        for old_frame in old_data.get("frames", []):
            try:
                new_frame = convert_frame_format(old_frame)
                new_data["frames"].append(new_frame)
                converted_count += 1
            except Exception as e:
                print(f"Error converting frame {old_frame.get('id', 'unknown')}: {e}")
        
        # Write converted data
        with open(output_file, 'w') as f:
            json.dump(new_data, f, indent=2)
        
        print("SUCCESS: Conversion complete!")
        print(f"   Original frames: {len(old_data.get('frames', []))}")
        print(f"   Converted frames: {converted_count}")
        print(f"   Output file: {output_file}")
        
        return True
        
    except Exception as e:
        print(f"ERROR: Conversion failed: {e}")
        return False

if __name__ == "__main__":
    input_file = "C:/Users/hugov/ClaudeProjects/2509_EtabsMCP_0.3/etabs_frames_enhanced.json"
    output_file = "C:/Users/hugov/ClaudeProjects/2510_TeklaMCP_0.3/etabs_300_frames_converted.json"
    
    convert_etabs_export(input_file, output_file)