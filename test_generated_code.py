#!/usr/bin/env python3
"""Extract the generated code to see what's on line 1236"""
import requests
import json

# Trigger extraction to get the generated code
response = requests.post(
    "http://localhost:5050/extract-drawing-objects",
    json={
        "drawing_mark": "B18",
        "max_drawings": 1,
        "discovery_mode": False,
        "extract_solid_vertices": False
    }
)

result = response.json()
print("Response:", json.dumps(result, indent=2)[:500])

# The error message contains the line number
if not result['success']:
    error = result.get('data', {}).get('result', '')
    if 'Line 1236' in error:
        print("\n✗ ERROR ON LINE 1236 in generated code")
        print("The problem is in the C# code string returned by GenerateFullExtractionCode()")
        print("\nWe need to find where ArrayList is incorrectly used as an IEnumerator")
