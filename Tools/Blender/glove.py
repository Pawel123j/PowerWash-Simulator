"""
Kuba's blue nitrile glove for ZMYWAK, modelled in Blender around the very skeleton the game's hand rig uses
(KitchenArt.BuildHand), in the game's own numbers: Unity hand-local metres, x to the right, y up, z towards the
fingertips, a right hand palm down with its thumb on the left. Rotations are composed the way Unity's
Quaternion.Euler does (about z, then x, then y), and the numbers go out as they are - Unity reads the same
triangles as front-facing.

The palm is a tapered, slightly domed slab, bevelled and subdivided; each finger and the thumb a Skin-modifier tube
along its joints, swelling at the knuckles and tapering to a rounded tip, subdivided smooth. Unity (GloveMesh) skins
it to the rig's joints, so every pose the hand already has still works.

Run headless (Blender 4 or 5):
    blender --background --factory-startup --python glove.py -- <out.json>
Writes vertices, normals, triangles and the part each vertex belongs to (0 palm, 1-4 the fingers from the index
to the little finger, 5 the thumb).
"""
import bpy
import bmesh
import json
import math
import sys
from mathutils import Matrix, Vector

# --- The rig, as KitchenArt.BuildHand builds it -----------------------------------------------------------------
PALM_Y = 0.056
FINGER_X = (-0.029, -0.0095, 0.0095, 0.028)
FINGER_YAW = (-3.0, 0.0, 2.0, 6.0)
FINGER_RADIUS = (0.0085, 0.009, 0.0085, 0.0075)
FINGER_LENGTHS = ((0.042, 0.025, 0.021), (0.046, 0.028, 0.023), (0.043, 0.026, 0.022), (0.034, 0.02, 0.019))
THUMB_BASE = Vector((-0.034, PALM_Y - 0.006, -0.048))
THUMB_EULER = (22.0, 0.0, -20.0)
THUMB_LENGTHS = (0.036, 0.03, 0.025)
THUMB_RADIUS = 0.0105


def unity_rotation(x, y, z):
    """Quaternion.Euler(x, y, z) as a matrix: about z first, then x, then y."""
    return (Matrix.Rotation(math.radians(y), 3, 'Y') @ Matrix.Rotation(math.radians(x), 3, 'X')
            @ Matrix.Rotation(math.radians(z), 3, 'Z'))


def link(obj):
    bpy.context.scene.collection.objects.link(obj)
    return obj


SHRINK = 1.22  # a Skin tube's square section, subdivided, comes out this much thinner than its radius


def skin_tube(name, points, levels=2):
    """A smooth tube through (position, radius) points, rooted at the first; a radius is a number, or (across, up)
    for a flattened section."""
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata([p for p, _ in points], [(i, i + 1) for i in range(len(points) - 1)], [])
    obj = link(bpy.data.objects.new(name, mesh))
    skin = obj.modifiers.new("Skin", 'SKIN')
    skin.use_smooth_shade = True
    skin.branch_smoothing = 0.0
    if len(mesh.skin_vertices) == 0:
        mesh.skin_vertices.new()
    layer = mesh.skin_vertices[0]
    for i, (_, r) in enumerate(points):
        across, up = (r, r) if isinstance(r, float) else r
        layer.data[i].radius = (across * SHRINK, up * SHRINK)
        layer.data[i].use_root = i == 0
    sub = obj.modifiers.new("Subdivision", 'SUBSURF')
    sub.levels = levels
    sub.render_levels = levels
    return obj


def finger(f):
    """From inside the palm, over the knuckle, through the two finger joints to the tip."""
    r = FINGER_RADIUS[f]
    lengths = FINGER_LENGTHS[f]
    d = unity_rotation(0.0, FINGER_YAW[f], 0.0) @ Vector((0.0, 0.0, 1.0))
    j0 = Vector((FINGER_X[f], PALM_Y - 0.002, 0.0265))
    j1 = j0 + d * (lengths[0] - 0.6 * r)
    j2 = j1 + d * (lengths[1] - 0.6 * r)
    tip = j2 + d * lengths[2]
    points = [
        (j0 - d * 0.014, r * 1.05),
        (j0, r * 1.1),                          # the knuckle
        ((j0 + j1) * 0.5, r * 0.97),
        (j1, r * 1.0),
        ((j1 + j2) * 0.5, r * 0.92),
        (j2, r * 0.93),
        (j2 + d * (lengths[2] * 0.5), r * 0.88),
        (tip - d * (r * 0.55), r * 0.8),        # subdivision rounds the cap off just short of the rig's tip
    ]
    return skin_tube(f"Finger{f}", points)


def thumb():
    d = unity_rotation(*THUMB_EULER) @ Vector((0.0, 0.0, 1.0))
    t0 = THUMB_BASE
    t1 = t0 + d * (THUMB_LENGTHS[0] - 0.006)
    t2 = t1 + d * (THUMB_LENGTHS[1] - 0.006)
    tip = t2 + d * THUMB_LENGTHS[2]
    points = [
        (t0 - d * 0.01, 0.0135),                # the ball of the thumb, in the palm's side
        (t0, 0.0128),
        ((t0 + t1) * 0.5, 0.0118),
        (t1, 0.0112),
        ((t1 + t2) * 0.5, 0.0104),
        (t2, 0.0102),
        (t2 + d * (THUMB_LENGTHS[2] * 0.5), 0.0096),
        (tip - d * 0.006, 0.0088),
    ]
    return skin_tube("Thumb", points)


def palm():
    """The back of the hand: a flattened tube from the wrist, widening to the knuckles, rounded all round - a touch
    thicker on the thumb's side is left to the thumb's own ball."""
    points = [
        (Vector((0.0, PALM_Y - 0.003, -0.074)), (0.026, 0.0155)),  # the wrist, inside the cuff
        (Vector((0.0, PALM_Y - 0.002, -0.05)), (0.032, 0.015)),
        (Vector((0.0, PALM_Y - 0.001, -0.015)), (0.0385, 0.0135)),
        # Across the knuckles: no higher than the fingers' own knuckles, or the back of the hand stands over them.
        (Vector((0.0, PALM_Y - 0.002, 0.018)), (0.041, 0.0108)),
    ]
    return skin_tube("Palm", points, levels=3)


def main():
    out_path = sys.argv[sys.argv.index("--") + 1]
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj)

    parts = [(0, palm())] + [(f + 1, finger(f)) for f in range(4)] + [(5, thumb())]
    depsgraph = bpy.context.evaluated_depsgraph_get()

    vertices, normals, triangles, part_of = [], [], [], []
    for part, obj in parts:
        evaluated = obj.evaluated_get(depsgraph)
        mesh = evaluated.to_mesh()
        bm = bmesh.new()
        bm.from_mesh(mesh)
        bmesh.ops.triangulate(bm, faces=bm.faces[:])
        bm.normal_update()
        bm.verts.index_update()
        base = len(part_of)
        for v in bm.verts:
            vertices.extend(round(c, 6) for c in v.co)
            normals.extend(round(c, 5) for c in v.normal)
            part_of.append(part)
        for face in bm.faces:
            triangles.extend(base + v.index for v in face.verts)
        bm.free()
        evaluated.to_mesh_clear()

    with open(out_path, "w") as out:
        json.dump({"source": "Tools/Blender/glove.py", "vertices": vertices, "normals": normals,
                   "triangles": triangles, "parts": part_of}, out, separators=(",", ":"))
    print(f"GLOVE {len(part_of)} vertices, {len(triangles) // 3} triangles -> {out_path}")

    if "--preview" in sys.argv:
        preview(sys.argv[sys.argv.index("--preview") + 1], [obj for _, obj in parts])


def preview(path, objects):
    """A quick look in Blender's Workbench: the glove from above and behind, about as the kitchen camera sees it."""
    material = bpy.data.materials.new("Nitrile")
    material.diffuse_color = (0.17, 0.37, 0.8, 1.0)
    for obj in objects:
        obj.data.materials.append(material)
    camera_data = bpy.data.cameras.new("Camera")
    camera_data.lens = 60.0
    camera = link(bpy.data.objects.new("Camera", camera_data))
    # The kitchen camera looks down at ~55 degrees from behind the wrist. (The numbers are Unity's, y up; Blender
    # reads them right-handed, so this picture is the game's mirror image - the thumb shows on the right.)
    camera.location = Vector((0.0, 0.3, -0.21))
    target = Vector((0.0, 0.05, 0.0))
    forward = (target - camera.location).normalized()
    right = forward.cross(Vector((0.0, 1.0, 0.0))).normalized()
    up = right.cross(forward)
    camera.matrix_world = Matrix((
        (right.x, up.x, -forward.x, camera.location.x),
        (right.y, up.y, -forward.y, camera.location.y),
        (right.z, up.z, -forward.z, camera.location.z),
        (0.0, 0.0, 0.0, 1.0)))
    scene = bpy.context.scene
    scene.camera = camera
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.display.shading.light = 'STUDIO'
    scene.display.shading.color_type = 'MATERIAL'
    scene.display.shading.show_cavity = True
    scene.render.resolution_x = 900
    scene.render.resolution_y = 900
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print(f"PREVIEW -> {path}")


main()
