"""Render all 19 ZMYWAK portraits in Blender with transparent backgrounds.

    blender --background --factory-startup --python portraits.py -- <output> [--only kuba,declan]

See README.md beside this file for source scenes, previews and render options.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from portraits_render import main

if __name__ == '__main__':
    main()
