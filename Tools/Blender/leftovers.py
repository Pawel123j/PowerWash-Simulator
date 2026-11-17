"""
The scraps left on ZMYWAK's dirty dishes, modelled in Blender: what was eaten off each kind of dish, heaped where it
sat, instead of the game's coloured lumps.
  - the plate: penne in a smear of tomato sauce, and a basil leaf;
  - the pan: fried potato cubes, slices of sausage and strips of browned onion;
  - the glass: a slice of lemon;
  - the bowl: a clump of porridge, a smear of it and raisins;
  - the mug: a squeezed-out tea bag, its string and tag;
  - the pot: stew stuck to the bottom - gravy with beef, potato and carrot;
  - the roaster: what is left of a lasagne - torn sheets in sauce under melted cheese.

Built in the game's numbers (Unity: y up), a heap about one unit across sitting on y = 0, which the game scales to
the dish. Every piece takes its colour from a small palette (one column per food, see PALETTE); the shading inside
it - darker where pieces touch, in the pasta's ridges, under the heap - is ambient occlusion baked by Cycles on the
GPU (OptiX where there is an RTX card) and goes out as the palette row (dark to light).

Run headless (Blender 4 or 5):
    blender --background --factory-startup --python leftovers.py -- <out folder> [--preview <png>]
Writes Leftovers<Dish>.json for each dish: vertices, normals, triangles, uv (palette column, shade) and the palette
(r, g, b of each food in turn).
"""
import bpy
import bmesh
import json
import math
import os
import random
import sys
from mathutils import Matrix, Vector

# Linear-ish base colours of the foods; the game shades each from dark (in occlusion) to a touch above it.
PALETTE = [
    ("tomato sauce", (0.48, 0.09, 0.03)),
    ("pasta", (0.8, 0.62, 0.32)),
    ("basil", (0.14, 0.33, 0.08)),
    ("fried potato", (0.72, 0.46, 0.15)),
    ("sausage", (0.45, 0.19, 0.11)),
    ("browned onion", (0.55, 0.32, 0.12)),
    ("lemon rind", (0.92, 0.72, 0.1)),
    ("lemon flesh", (0.95, 0.86, 0.48)),
    ("pasta in sauce", (0.68, 0.24, 0.07)),
    ("browned potato", (0.45, 0.24, 0.07)),
    ("porridge", (0.78, 0.68, 0.46)),
    ("raisin", (0.22, 0.08, 0.05)),
    ("tea bag paper", (0.88, 0.84, 0.74)),
    ("tea stain", (0.5, 0.28, 0.12)),
    ("gravy", (0.36, 0.18, 0.08)),
    ("carrot", (0.88, 0.4, 0.08)),
    ("beef", (0.38, 0.18, 0.11)),
    ("lasagne sheet", (0.84, 0.68, 0.38)),
    ("melted cheese", (0.9, 0.66, 0.24)),
]
(SAUCE, PASTA, BASIL, POTATO, SAUSAGE, ONION, RIND, FLESH, SAUCED, BROWNED,
 PORRIDGE, RAISIN, PAPER, TEA, GRAVY, CARROT, BEEF, SHEET, CHEESE) = range(len(PALETTE))


class Heap:
    """Vertices, faces and each vertex's palette column, built piece by piece."""

    def __init__(self):
        self.verts = []
        self.faces = []
        self.column = []

    def add(self, bm, column, transform=Matrix.Identity(4), column_of=None, closed=True):
        """Adds a bmesh (its faces triangulated later), moved by `transform`; `column_of(local position)` may pick
        a column per vertex instead of one for the piece. A closed piece has its faces turned outwards first."""
        if closed:
            bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        base = len(self.verts)
        bm.verts.index_update()
        for v in bm.verts:
            self.verts.append(transform @ v.co)
            self.column.append(column_of(v.co) if column_of else column)
        for f in bm.faces:
            self.faces.append([base + v.index for v in f.verts])
        bm.free()

    def add_object(self, obj, column, transform=Matrix.Identity(4), column_of=None):
        """Adds an object with its modifiers applied (metaballs, curves, subdivided pieces)."""
        depsgraph = bpy.context.evaluated_depsgraph_get()
        evaluated = obj.evaluated_get(depsgraph)
        mesh = evaluated.to_mesh()
        bm = bmesh.new()
        bm.from_mesh(mesh)
        evaluated.to_mesh_clear()
        self.add(bm, column, transform, column_of)
        bpy.data.objects.remove(obj)


def place(x, z, yaw, y=0.0, tilt_x=0.0, tilt_z=0.0):
    """A piece's transform: lying on the dish at (x, z), turned by yaw about the up axis (y), tilted a little."""
    return (Matrix.Translation((x, y, z)) @ Matrix.Rotation(math.radians(yaw), 4, 'Y')
            @ Matrix.Rotation(math.radians(tilt_x), 4, 'X') @ Matrix.Rotation(math.radians(tilt_z), 4, 'Z'))


def link(obj):
    bpy.context.scene.collection.objects.link(obj)
    return obj


def smooth_object(name, bm, bevel=0.0, levels=2):
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    for poly in mesh.polygons:
        poly.use_smooth = True
    obj = link(bpy.data.objects.new(name, mesh))
    if bevel > 0.0:
        mod = obj.modifiers.new("Bevel", 'BEVEL')
        mod.width = bevel
        mod.segments = 2
    sub = obj.modifiers.new("Subdivision", 'SUBSURF')
    sub.levels = levels
    sub.render_levels = levels
    return obj


# --- The pieces (each built lying along x, its up along y, centred on its own origin) ---------------------------

def penne(rng, length=0.4, radius=0.058):
    """A ridged tube cut on the slant at both ends, lying along x."""
    bm = bmesh.new()
    rings, sides = 9, 14
    grid = []
    for r in range(rings):
        x = -length * 0.5 + length * r / (rings - 1)
        ring = []
        for s in range(sides):
            a = 2.0 * math.pi * s / sides
            rr = radius * (1.0 + 0.07 * math.cos(a * 7.0))                  # the ridges
            y, z = math.sin(a) * rr, math.cos(a) * rr
            ring.append(bm.verts.new((x + 0.75 * z, y + radius, z)))         # sheared: the slanted cuts
        grid.append(ring)
    for r in range(rings - 1):
        for s in range(sides):
            bm.faces.new((grid[r][s], grid[r][(s + 1) % sides], grid[r + 1][(s + 1) % sides], grid[r + 1][s]))
    for ring, flip in ((grid[0], True), (grid[-1], False)):
        face = bm.faces.new(ring[::-1] if flip else ring)
        bmesh.ops.triangulate(bm, faces=[face])
    bm.normal_update()
    return bm


def leaf(length=0.34, width=0.15):
    """A basil leaf: pointed oval with its edges curling down and a crease along the middle."""
    bm = bmesh.new()
    nx, nz = 10, 6
    grid = []
    for i in range(nx + 1):
        t = i / nx
        half = width * 0.5 * math.sin(math.pi * min(1.0, t * 1.08)) ** 0.8
        row = []
        for j in range(nz + 1):
            s = j / nz * 2.0 - 1.0
            x = (t - 0.5) * length
            z = s * half
            y = 0.012 - 0.03 * s * s + 0.006 * abs(s) + 0.02 * math.sin(math.pi * t)
            row.append(bm.verts.new((x, y, z)))
        grid.append(row)
    for i in range(nx):
        for j in range(nz):
            bm.faces.new((grid[i][j], grid[i][j + 1], grid[i + 1][j + 1], grid[i + 1][j]))
    bm.normal_update()
    return bm


def sauce_smear(rng, centre, spread, blobs=8):
    """A flattened, uneven smear of sauce (metaballs, so it flows round itself)."""
    data = bpy.data.metaballs.new("Sauce")
    data.resolution = 0.025
    data.threshold = 0.6
    obj = link(bpy.data.objects.new("Sauce", data))
    for _ in range(blobs):
        e = data.elements.new(type='ELLIPSOID')
        a = rng.uniform(0.0, 2.0 * math.pi)
        d = rng.uniform(0.0, spread)
        e.co = (centre[0] + math.cos(a) * d, 0.0, centre[1] + math.sin(a) * d)
        e.radius = rng.uniform(0.16, 0.26)
        e.size_x = rng.uniform(1.0, 1.5)
        e.size_y = 0.22                                                       # flat: y is up
        e.size_z = rng.uniform(0.8, 1.2)
    return obj


def potato(rng, size):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=size)
    for v in bm.verts:
        v.co += Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), rng.uniform(-1, 1))) * size * 0.07
    return smooth_object("Potato", bm, bevel=size * 0.16)


def sausage_slice(radius=0.12, thickness=0.055):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=18, radius1=radius, radius2=radius, depth=thickness)
    bmesh.ops.rotate(bm, verts=bm.verts, cent=(0, 0, 0), matrix=Matrix.Rotation(math.radians(90.0), 3, 'X'))  # flat on y
    return smooth_object("Sausage", bm, bevel=thickness * 0.3, levels=1)


def onion_strip(rng):
    """A thin, curved strip of fried onion: a bent tube."""
    curve = bpy.data.curves.new("Onion", 'CURVE')
    curve.dimensions = '3D'
    curve.bevel_depth = 0.016
    curve.bevel_resolution = 2
    curve.use_fill_caps = True
    spline = curve.splines.new('POLY')
    arc = rng.uniform(1.4, 2.4)
    radius = rng.uniform(0.1, 0.16)
    points = 8
    spline.points.add(points - 1)
    for i, p in enumerate(spline.points):
        a = -arc * 0.5 + arc * i / (points - 1)
        p.co = (math.sin(a) * radius, 0.016 + 0.01 * math.sin(i), math.cos(a) * radius - radius, 1.0)
    return link(bpy.data.objects.new("Onion", curve))


def lemon_slice(radius=0.3, thickness=0.06):
    """Half a slice of lemon: rind round the arc, pale flesh with its segments' white walls."""
    bm = bmesh.new()
    arc_steps, rings = 16, 5
    top, bottom = [], []
    for level, store in ((thickness, top), (0.0, bottom)):
        centre = bm.verts.new((0.0, level, 0.0))
        rows = [[centre] * (arc_steps + 1)]
        for k in range(1, rings + 1):
            rr = radius * k / rings
            rows.append([bm.verts.new((math.cos(math.pi * i / arc_steps) * rr, level, math.sin(math.pi * i / arc_steps) * rr))
                         for i in range(arc_steps + 1)])
        store.extend(rows)
    for rows, up in ((top, True), (bottom, False)):
        for k in range(rings):
            for i in range(arc_steps):
                a, b, c, d = rows[k][i], rows[k][i + 1], rows[k + 1][i + 1], rows[k + 1][i]
                quad = [a, d, c, b] if up else [a, b, c, d]
                quad = [v for n, v in enumerate(quad) if v not in quad[:n]]   # the centre is one vertex
                bm.faces.new(quad)
    for i in range(arc_steps):                                              # the rind's edge
        bm.faces.new((top[-1][i], top[-1][i + 1], bottom[-1][i + 1], bottom[-1][i]))
    for k in range(rings):                                                  # the cut face
        for side in (0, arc_steps):
            a, b = top[k][side], top[k + 1][side]
            c, d = bottom[k + 1][side], bottom[k][side]
            quad = [a, b, c, d] if side == arc_steps else [a, d, c, b]
            quad = [v for n, v in enumerate(quad) if v not in quad[:n]]
            if len(quad) >= 3:
                bm.faces.new(quad)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm


# --- The heaps --------------------------------------------------------------------------------------------------

def plate_heap(rng):
    heap = Heap()
    heap.add_object(sauce_smear(rng, (0.0, 0.0), 0.2, blobs=9), SAUCE)
    for i in range(6):
        a = rng.uniform(0.0, 2.0 * math.pi)
        d = rng.uniform(0.05, 0.3)
        # The underside, where it lay in the sauce, is coated (the tube's middle is at y = its radius).
        heap.add(penne(rng), PASTA, place(math.cos(a) * d, math.sin(a) * d, rng.uniform(0, 360), y=0.01,
                                          tilt_z=rng.uniform(-8, 8)),
                 column_of=lambda p: SAUCED if p.y < 0.05 + 0.01 * math.sin(p.x * 40.0) else PASTA)
    heap.add(leaf(), BASIL, place(0.12, -0.18, rng.uniform(0, 360), y=0.07, tilt_x=rng.uniform(-10, 10)), closed=False)
    return heap


def pan_heap(rng):
    heap = Heap()
    for i in range(6):
        a = rng.uniform(0.0, 2.0 * math.pi)
        d = rng.uniform(0.0, 0.3)
        size = rng.uniform(0.13, 0.17)
        # Fried: the corners and edges browned where they touched the pan.
        heap.add_object(potato(rng, size), POTATO, place(math.cos(a) * d, math.sin(a) * d, rng.uniform(0, 360),
                                                       y=size * 0.45, tilt_x=rng.uniform(-15, 15), tilt_z=rng.uniform(-15, 15)),
                        column_of=lambda p, s=size: BROWNED if (abs(p.x) + abs(p.y) + abs(p.z)) > 0.6 * s else POTATO)
    for i in range(3):
        a = rng.uniform(0.0, 2.0 * math.pi)
        d = rng.uniform(0.15, 0.36)
        heap.add_object(sausage_slice(), SAUSAGE, place(math.cos(a) * d, math.sin(a) * d, rng.uniform(0, 360), y=0.03,
                                                          tilt_x=rng.uniform(-12, 12)))
    for i in range(5):
        a = rng.uniform(0.0, 2.0 * math.pi)
        d = rng.uniform(0.05, 0.38)
        heap.add_object(onion_strip(rng), ONION, place(math.cos(a) * d, math.sin(a) * d, rng.uniform(0, 360), y=0.005))
    return heap


def glass_heap(rng):
    heap = Heap()
    bm = lemon_slice()
    heap.add(bm, RIND, place(0.0, -0.12, rng.uniform(0, 360), y=0.0, tilt_x=6),
             column_of=lambda p: RIND if Vector((p.x, p.z)).length > 0.27 else FLESH)
    return heap


def blob(name, rng, centre, spread, count, flat, radius=(0.16, 0.26)):
    """A lump or smear of something soft (metaballs, so it flows into itself): `flat` is its height to its width."""
    data = bpy.data.metaballs.new(name)
    data.resolution = 0.025
    data.threshold = 0.6
    obj = link(bpy.data.objects.new(name, data))
    for _ in range(count):
        e = data.elements.new(type='ELLIPSOID')
        a = rng.uniform(0.0, 2.0 * math.pi)
        d = rng.uniform(0.0, spread)
        e.co = (centre[0] + math.cos(a) * d, 0.0, centre[1] + math.sin(a) * d)
        e.radius = rng.uniform(*radius)
        e.size_x = rng.uniform(0.9, 1.3)
        e.size_y = flat
        e.size_z = rng.uniform(0.9, 1.3)
    return obj


def bead(radius, squash=1.0, segments=10, rings=6):
    """A small lumpy sphere (a raisin): squashed in y by `squash`."""
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=segments, v_segments=rings, radius=radius)
    for v in bm.verts:
        v.co = Vector((v.co.x * (1.0 + 0.15 * math.sin(v.co.y * 90.0)), v.co.z * squash, v.co.y))  # Blender's sphere is z-up
    return bm


def bowl_heap(rng):
    """A clump of porridge where the spoon left it, a thinner smear of it, and raisins."""
    heap = Heap()
    heap.add_object(blob("Porridge", rng, (0.02, 0.0), 0.12, 7, flat=0.55, radius=(0.18, 0.26)), PORRIDGE)
    heap.add_object(blob("Smear", rng, (-0.22, 0.16), 0.12, 5, flat=0.2, radius=(0.12, 0.18)), PORRIDGE)
    for i in range(7):
        a = rng.uniform(0.0, 2.0 * math.pi)
        d = rng.uniform(0.04, 0.3)
        heap.add(bead(0.035, squash=0.6), RAISIN, place(math.cos(a) * d, math.sin(a) * d, rng.uniform(0, 360),
                                                     y=0.09 if d < 0.2 else 0.03))
    return heap


def tea_bag(rng, width=0.42, depth=0.32, puff=0.07):
    """A used tea bag: a puffy pouch, flatter at its sealed edges, wrinkled, stained where the tea soaked through."""
    bm = bmesh.new()
    nx, nz = 12, 9
    grid = {}
    for top in (True, False):
        for i in range(nx + 1):
            for j in range(nz + 1):
                u, v = i / nx * 2.0 - 1.0, j / nz * 2.0 - 1.0
                edge = max(abs(u), abs(v))
                height = puff * max(0.0, 1.0 - edge ** 3) * (1.0 + 0.25 * math.sin(u * 7.0 + v * 5.0))
                y = 0.004 + (height if top else -height * 0.25)
                grid[(top, i, j)] = bm.verts.new((u * width * 0.5, y + puff * 0.3, v * depth * 0.5))
    for top in (True, False):
        for i in range(nx):
            for j in range(nz):
                quad = [grid[(top, i, j)], grid[(top, i, j + 1)], grid[(top, i + 1, j + 1)], grid[(top, i + 1, j)]]
                bm.faces.new(quad if top else quad[::-1])
    # The sealed edge: round the border the two sheets meet at the same points - weld them into one closed pouch.
    bmesh.ops.remove_doubles(bm, verts=bm.verts[:], dist=1e-5)
    return bm


def tea_tag(width=0.12, depth=0.09, thickness=0.006):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        v.co = Vector((v.co.x * width, v.co.y * thickness + thickness * 0.5, v.co.z * depth))
    return bm


def string_curve(rng, start, end):
    """The tea bag's string, lying slack on the bottom between the bag and its tag."""
    curve = bpy.data.curves.new("String", 'CURVE')
    curve.dimensions = '3D'
    curve.bevel_depth = 0.005
    curve.bevel_resolution = 1
    spline = curve.splines.new('POLY')
    points = 10
    spline.points.add(points - 1)
    for i, p in enumerate(spline.points):
        t = i / (points - 1)
        x = start[0] + (end[0] - start[0]) * t + 0.05 * math.sin(t * math.pi * 2.0 + rng.uniform(0, 1))
        z = start[1] + (end[1] - start[1]) * t + 0.04 * math.sin(t * math.pi * 3.0)
        p.co = (x, 0.006, z, 1.0)
    return link(bpy.data.objects.new("String", curve))


def mug_heap(rng):
    """A squeezed-out tea bag at the bottom of the mug, its string trailing to the paper tag."""
    heap = Heap()
    heap.add(tea_bag(rng), PAPER, place(-0.05, 0.0, rng.uniform(0, 360), y=0.0),
             column_of=lambda p: TEA if abs(p.x) < 0.15 and abs(p.z) < 0.11 and p.y > 0.02 else PAPER)
    heap.add_object(string_curve(rng, (0.12, 0.05), (0.34, -0.18)), PAPER)
    heap.add(tea_tag(), PAPER, place(0.36, -0.2, rng.uniform(0, 360)))
    return heap


def carrot_slice(radius=0.075, thickness=0.03):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=14, radius1=radius, radius2=radius * 0.95, depth=thickness)
    bmesh.ops.rotate(bm, verts=bm.verts, cent=(0, 0, 0), matrix=Matrix.Rotation(math.radians(90.0), 3, 'X'))
    return bm


def lasagne_sheet(rng, length=0.34, width=0.2, thickness=0.018):
    """A torn piece of lasagne sheet: a thin slab with one wavy (ruffled) edge, sagging a little."""
    bm = bmesh.new()
    nx, nz = 10, 5
    grid = {}
    for top in (True, False):
        for i in range(nx + 1):
            for j in range(nz + 1):
                u, v = i / nx - 0.5, j / nz - 0.5
                ruffle = 0.02 * math.sin(u * 40.0) * max(0.0, v) * 2.0         # the ruffled long edge
                sag = -0.03 * (u * u * 4.0)
                y = sag + ruffle + (thickness if top else 0.0) + thickness
                grid[(top, i, j)] = bm.verts.new((u * length, y, v * width + (0.02 * math.sin(u * 9.0 + rng.uniform(0, 1)) if j == 0 else 0.0)))
    for top in (True, False):
        for i in range(nx):
            for j in range(nz):
                quad = [grid[(top, i, j)], grid[(top, i, j + 1)], grid[(top, i + 1, j + 1)], grid[(top, i + 1, j)]]
                bm.faces.new(quad if top else quad[::-1])
    rim = [(i, 0) for i in range(nx)] + [(nx, j) for j in range(nz)] + [(i, nz) for i in range(nx, 0, -1)] + [(0, j) for j in range(nz, 0, -1)]
    for k in range(len(rim)):
        a, b = rim[k], rim[(k + 1) % len(rim)]
        bm.faces.new((grid[(True, a[0], a[1])], grid[(True, b[0], b[1])], grid[(False, b[0], b[1])], grid[(False, a[0], a[1])]))
    return bm


def roaster_heap(rng):
    """What is left of a lasagne: torn sheets in a smear of tomato sauce, with melted cheese run over them."""
    heap = Heap()
    heap.add_object(blob("Sauce", rng, (0.0, 0.0), 0.28, 9, flat=0.2, radius=(0.15, 0.22)), SAUCE)
    for i in range(4):
        a = rng.uniform(0.0, 2.0 * math.pi)
        d = rng.uniform(0.0, 0.25)
        heap.add(lasagne_sheet(rng), SHEET, place(math.cos(a) * d, math.sin(a) * d, rng.uniform(0, 360), y=0.02,
                                                  tilt_x=rng.uniform(-10, 10), tilt_z=rng.uniform(-10, 10)))
    heap.add_object(blob("Cheese", rng, (0.05, -0.05), 0.22, 7, flat=0.35, radius=(0.08, 0.13)), CHEESE)
    return heap


def pot_heap(rng):
    """Stew stuck to the bottom: a pool of gravy with chunks of beef, potato and slices of carrot in it."""
    heap = Heap()
    heap.add_object(blob("Gravy", rng, (0.0, 0.0), 0.25, 10, flat=0.18, radius=(0.16, 0.24)), GRAVY)
    for i in range(5):
        a = rng.uniform(0.0, 2.0 * math.pi)
        d = rng.uniform(0.0, 0.3)
        size = rng.uniform(0.11, 0.15)
        heap.add_object(potato(rng, size), BEEF, place(math.cos(a) * d, math.sin(a) * d, rng.uniform(0, 360),
                                                     y=size * 0.4, tilt_x=rng.uniform(-20, 20), tilt_z=rng.uniform(-20, 20)))
    for i in range(3):
        a = rng.uniform(0.0, 2.0 * math.pi)
        d = rng.uniform(0.05, 0.32)
        size = rng.uniform(0.12, 0.15)
        heap.add_object(potato(rng, size), POTATO, place(math.cos(a) * d, math.sin(a) * d, rng.uniform(0, 360),
                                                       y=size * 0.4, tilt_x=rng.uniform(-15, 15)))
    for i in range(6):
        a = rng.uniform(0.0, 2.0 * math.pi)
        d = rng.uniform(0.05, 0.38)
        heap.add(carrot_slice(), CARROT, place(math.cos(a) * d, math.sin(a) * d, rng.uniform(0, 360), y=0.03,
                                               tilt_x=rng.uniform(-25, 25)))
    return heap


# --- Baking and export ------------------------------------------------------------------------------------------

def enable_gpu():
    prefs = bpy.context.preferences.addons["cycles"].preferences
    for backend in ("OPTIX", "CUDA"):
        try:
            prefs.compute_device_type = backend
            prefs.get_devices()
            gpus = [d for d in prefs.devices if d.type == backend]
            if gpus:
                for d in prefs.devices:
                    d.use = d.type == backend
                print(f"LEFTOVERS baking on {gpus[0].name} ({backend})")
                return 'GPU'
        except Exception:
            pass
    print("LEFTOVERS baking on the CPU")
    return 'CPU'


def bake_occlusion(heap):
    """One mesh of the whole heap on a dish floor; Cycles bakes ambient occlusion into a colour per vertex."""
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj)
    mesh = bpy.data.meshes.new("Heap")
    mesh.from_pydata([tuple(v) for v in heap.verts], [], heap.faces)
    mesh.validate()
    for poly in mesh.polygons:
        poly.use_smooth = True
    obj = link(bpy.data.objects.new("Heap", mesh))
    floor = bpy.data.meshes.new("Floor")
    floor.from_pydata([(-2, 0, -2), (2, 0, -2), (2, 0, 2), (-2, 0, 2)], [], [(0, 3, 2, 1)])  # facing up (y)
    link(bpy.data.objects.new("Floor", floor))

    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.device = enable_gpu()
    scene.cycles.samples = 128
    scene.world = scene.world or bpy.data.worlds.new("World")
    scene.world.light_settings.distance = 0.12
    attribute = mesh.color_attributes.new("AO", 'FLOAT_COLOR', 'POINT')
    mesh.color_attributes.active_color = attribute
    scene.render.bake.target = 'VERTEX_COLORS'
    for o in scene.objects:
        o.select_set(o == obj)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.bake(type='AO')
    return obj


def export(heap, obj, path, rng):
    mesh = obj.data
    ao = [mesh.color_attributes["AO"].data[i].color[0] for i in range(len(mesh.vertices))]
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    bm.verts.index_update()
    bm.normal_update()
    columns = len(PALETTE)
    vertices, normals, uv, triangles = [], [], [], []
    for v in bm.verts:
        vertices.extend(round(c, 5) for c in v.co)
        normals.extend(round(c, 4) for c in v.normal)
        shade = max(0.0, min(1.0, ao[v.index] * rng.uniform(0.92, 1.04)))
        uv.extend((round((heap.column[v.index] + 0.5) / columns, 5), round(shade, 4)))
    for face in bm.faces:
        triangles.extend(v.index for v in face.verts)
    bm.free()
    with open(path, "w") as out:
        json.dump({"source": "Tools/Blender/leftovers.py", "vertices": vertices, "normals": normals, "uv": uv,
                   "triangles": triangles, "palette": [x for _, c in PALETTE for x in c]}, out, separators=(",", ":"))
    print(f"LEFTOVERS {os.path.basename(path)}: {len(vertices) // 3} vertices, {len(triangles) // 3} triangles")


def preview(path, objects_by_dish):
    """The three heaps side by side in Workbench, palette colours times the baked occlusion."""
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj)
    for k, (heap, obj_data) in enumerate(objects_by_dish):
        verts = [Vector(v) + Vector(((k - 1) * 1.3, 0, 0)) for v in heap.verts]
        mesh = bpy.data.meshes.new(f"Preview{k}")
        mesh.from_pydata([tuple(v) for v in verts], [], heap.faces)
        colours = mesh.color_attributes.new("Col", 'FLOAT_COLOR', 'POINT')
        mesh.color_attributes.active_color = colours
        for i, column in enumerate(heap.column):
            c = PALETTE[column][1]
            s = obj_data[i]
            colours.data[i].color = (c[0] * s, c[1] * s, c[2] * s, 1.0)
        for poly in mesh.polygons:
            poly.use_smooth = True
        link(bpy.data.objects.new(f"Preview{k}", mesh))
    camera = link(bpy.data.objects.new("Camera", bpy.data.cameras.new("Camera")))
    camera.data.lens = 50.0
    eye, target = Vector((0.0, 2.6, -2.2)), Vector((0.0, 0.0, 0.0))
    forward = (target - eye).normalized()
    right = forward.cross(Vector((0.0, 1.0, 0.0))).normalized()
    up = right.cross(forward)
    camera.matrix_world = Matrix(((right.x, up.x, -forward.x, eye.x), (right.y, up.y, -forward.y, eye.y),
                                  (right.z, up.z, -forward.z, eye.z), (0, 0, 0, 1)))
    scene = bpy.context.scene
    scene.camera = camera
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.display.shading.light = 'STUDIO'
    scene.display.shading.color_type = 'VERTEX'
    scene.render.resolution_x, scene.render.resolution_y = 1200, 600
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print(f"PREVIEW -> {path}")


def main():
    args = sys.argv[sys.argv.index("--") + 1:]
    out_dir = args[0]
    preview_path = args[args.index("--preview") + 1] if "--preview" in args else None
    shown = []
    heaps = (("Plate", plate_heap, 7), ("Pan", pan_heap, 11), ("Glass", glass_heap, 3),
             ("Bowl", bowl_heap, 5), ("Mug", mug_heap, 9), ("Pot", pot_heap, 13), ("Roaster", roaster_heap, 17))
    for name, build, seed in heaps:
        for obj in list(bpy.data.objects):
            bpy.data.objects.remove(obj)
        rng = random.Random(seed)
        heap = build(rng)
        obj = bake_occlusion(heap)
        export(heap, obj, os.path.join(out_dir, f"Leftovers{name}.json"), random.Random(seed + 100))
        shown.append((heap, [obj.data.color_attributes["AO"].data[i].color[0] for i in range(len(obj.data.vertices))]))
    if preview_path:
        preview(preview_path, shown)


main()
