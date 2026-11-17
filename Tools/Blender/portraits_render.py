"""ZMYWAK portrait studio: Cycles renders and editable per-character .blend sources."""
import argparse
import json
import os
import sys
import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from portrait_catalogue import PEOPLE
from portrait_model import Person


def gpu_device():
    prefs = bpy.context.preferences.addons['cycles'].preferences
    for backend in ('OPTIX', 'CUDA'):
        try:
            prefs.compute_device_type = backend
            prefs.get_devices()
            if any(d.type == backend for d in prefs.devices):
                for d in prefs.devices:
                    d.use = d.type == backend
                print('PORTRAITS device:', backend, flush=True)
                return 'GPU'
        except Exception:
            pass
    return 'CPU'


def area(name, co, target, energy, color, size):
    data = bpy.data.lights.new(name, 'AREA')
    data.energy, data.color, data.shape, data.size = energy, color, 'DISK', size
    obj = bpy.data.objects.new(name, data)
    bpy.context.scene.collection.objects.link(obj)
    obj.location = co
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()


def stage(args):
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.device = gpu_device()
    scene.cycles.samples = args.samples
    scene.cycles.use_denoising = True
    scene.cycles.max_bounces = 6
    scene.render.resolution_x = scene.render.resolution_y = args.size
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = 'PNG'
    scene.render.image_settings.color_mode = 'RGBA'
    scene.render.image_settings.color_depth = '8'
    scene.view_settings.view_transform = args.view
    scene.view_settings.look = args.look
    scene.view_settings.exposure = args.exposure
    world = bpy.data.worlds.new('Portrait studio / cool ambient')
    world.use_nodes = True
    world.node_tree.nodes['Background'].inputs['Color'].default_value = (.34,.4,.5,1)
    world.node_tree.nodes['Background'].inputs['Strength'].default_value = .28
    scene.world = world
    camera = bpy.data.cameras.new('Portrait 85mm')
    camera.type = 'ORTHO'
    camera.ortho_scale = .64
    obj = bpy.data.objects.new('Portrait camera', camera)
    scene.collection.objects.link(obj)
    obj.location = (0,-2,1.61)
    obj.rotation_euler = (Vector((0,0,1.483))-obj.location).to_track_quat('-Z','Y').to_euler()
    scene.camera = obj
    target = (0,0,1.56)
    area('Key / large warm softbox',(-.75,-1.1,2.35),target,42,(1,.88,.76),1.0)
    area('Fill / sky',(.8,-.7,1.7),target,16,(.74,.84,1),1.15)
    area('Rim / Dublin window',(.55,.55,2.05),target,48,(.76,.87,1),.75)
    area('Eye bounce',(-.1,-1.2,1.45),target,8,(1,.92,.85),.6)


def clear():
    for obj in list(bpy.data.objects):
        if obj.type not in ('CAMERA','LIGHT'):
            bpy.data.objects.remove(obj,do_unlink=True)
    for datablocks in (bpy.data.meshes,bpy.data.curves,bpy.data.materials):
        for block in list(datablocks):
            if block.users == 0:
                datablocks.remove(block)


def main():
    bpy.context.preferences.filepaths.save_version = 0
    paths = bpy.context.preferences.filepaths
    # Background exports do not need desktop file-browser thumbnails.
    preview = paths.bl_rna.properties.get('file_preview_type')
    if preview and 'NONE' in preview.enum_items.keys():
        paths.file_preview_type = 'NONE'
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output', nargs='?', default='Portraits')
    parser.add_argument('--only',help='Comma-separated speaker IDs')
    parser.add_argument('--samples',type=int,default=96)
    parser.add_argument('--size',type=int,default=512)
    parser.add_argument('--blend-dir',help='Also save editable Blender studio scenes outside Unity Assets')
    parser.add_argument('--manifest',help='Optional JSON render report path, outside Unity Assets')
    parser.add_argument('--view',default='Khronos PBR Neutral')
    parser.add_argument('--look',default='None')
    parser.add_argument('--exposure',type=float,default=-.7)
    argv=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
    args=parser.parse_args(argv)
    ids=args.only.split(',') if args.only else list(PEOPLE)
    unknown=set(ids)-PEOPLE.keys()
    if unknown: parser.error('Unknown speaker IDs: '+','.join(sorted(unknown)))
    args.output=os.path.abspath(args.output)
    os.makedirs(args.output,exist_ok=True)
    if args.blend_dir:
        args.blend_dir=os.path.abspath(args.blend_dir)
        os.makedirs(args.blend_dir,exist_ok=True)
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj,do_unlink=True)
    stage(args)
    manifest=[]
    for pid in ids:
        clear()
        Person(pid,PEOPLE[pid]).build()
        scene=bpy.context.scene
        scene.render.filepath=os.path.join(args.output,'Portrait_'+pid+'.png')
        if args.blend_dir:
            # Open the source in its lit camera composition, with all meshes editable.
            for screen in bpy.data.screens:
                for area_ in screen.areas:
                    if area_.type=='VIEW_3D':
                        area_.spaces.active.region_3d.view_perspective='CAMERA'
                        area_.spaces.active.shading.type='MATERIAL'
            output_path = scene.render.filepath
            scene.render.filepath = '//Portrait_'+pid+'.png'
            bpy.ops.wm.save_as_mainfile(filepath=os.path.join(args.blend_dir,'Portrait_'+pid+'.blend'),compress=True)
            scene.render.filepath = output_path
        bpy.ops.render.render(write_still=True)
        manifest.append({'id':pid,'file':os.path.basename(scene.render.filepath),'size':args.size,'samples':args.samples})
        print('PORTRAITS wrote '+scene.render.filepath,flush=True)
    if args.manifest:
        with open(os.path.abspath(args.manifest),'w',encoding='utf8') as f:
            json.dump(manifest,f,indent=2)


if __name__=='__main__':
    main()
