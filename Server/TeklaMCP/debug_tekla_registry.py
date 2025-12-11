#!/usr/bin/env python3
"""
Debug registry from Tekla server directory
"""

import sys
import os

# Add ubim_core to path
sys.path.append("../../../2511_UBIM_DataBridge")

from ubim_core.sync.universal_guid_registry import UniversalGUIDRegistry

def test_from_tekla_dir():
    print("=== TEKLA DIRECTORY REGISTRY TEST ===")
    print(f"Current directory: {os.getcwd()}")
    
    # Test the path the Tekla server should use
    registry_path = "../../../2511_UBIM_DataBridge/.ubim_universal_registry"
    print(f"Registry path: {registry_path}")
    print(f"Registry file exists: {os.path.exists(registry_path)}")
    
    if os.path.exists(registry_path):
        registry = UniversalGUIDRegistry(registry_path)
        print(f"Registry mappings loaded: {len(registry.mappings)}")
        
        # Test the same Universal GUID
        test_universal_guid = "12345678-1234-4567-8901-123456789abc"
        print(f"Universal GUID exists: {test_universal_guid in registry.mappings}")
        
        if test_universal_guid in registry.mappings:
            # Try registration
            test_tekla_guid = "34ec50cc-93b5-4831-b0a3-004a5136f8ee"
            success = registry.register_software_object("tekla", test_tekla_guid, test_universal_guid, "frame")
            print(f"Registration success: {success}")
        else:
            print("Universal GUID not found!")
    else:
        print("Registry file not found!")

if __name__ == "__main__":
    test_from_tekla_dir()