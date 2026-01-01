#!/usr/bin/env python3
"""Save the generated C# code to inspect it"""
import re

# Read the ProgramClean.cs file
with open('Server/TeklaMCP/ProgramClean.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Find the GenerateFullExtractionCode method
match = re.search(r'private static string GenerateFullExtractionCode.*?return @"(.*?)";', content, re.DOTALL)

if match:
    generated_code = match.group(1)

    # Save to file
    with open('generated_extraction_code.cs', 'w', encoding='utf-8') as f:
        f.write(generated_code)

    print(f"Generated code saved: {len(generated_code)} characters")
    print(f"Lines: {len(generated_code.splitlines())}")

    # Check for ArrayList.MoveNext patterns
    arraylist_movenext = re.findall(r'(\w+)\s+as\s+System\.Collections\.ArrayList.*?\.MoveNext\(\)', generated_code, re.DOTALL)
    if arraylist_movenext:
        print(f"\n⚠️ FOUND ArrayList.MoveNext() patterns:")
        for var in arraylist_movenext:
            print(f"  - Variable: {var}")
    else:
        print("\n✓ No ArrayList.MoveNext() patterns found")
else:
    print("Could not find GenerateFullExtractionCode method")
