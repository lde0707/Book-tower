"""Gently bend the upper paper hat backward; preserve brim and character V5."""
import bpy, math, sys, json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
base=(ROOT/'Tools/model_turnaround_spirit.py').read_text(encoding='utf-8').replace('TurnaroundSpiritModelV1','TurnaroundSpiritModelV6')
stage=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else 'model'
if stage=='bake':
    exec(compile(base.split('bpy.ops.wm.open_mainfile')[0],str(ROOT/'Tools/model_turnaround_spirit.py'),'exec'))
    raise SystemExit(0)
SRC=ROOT/'ArtSource/TurnaroundSpiritModelV6';OUT=ROOT/'Docs/ArtReview/TurnaroundSpiritModelV6'
SRC.mkdir(parents=True,exist_ok=True);OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/TurnaroundSpiritModelV5/PaperHoodSpirit.blend'))
scene=bpy.context.scene;root=bpy.data.objects['PaperHoodSpirit']
prefs=bpy.context.preferences.addons['cycles'].preferences
prefs.compute_device_type='OPTIX';prefs.get_devices()
for device in prefs.devices:device.use=device.type!='CPU'
scene.cycles.device='GPU'
def smooth(t):
    t=max(0,min(1,t));return t*t*(3-2*t)
changed=0
for o in root.children:
    if o.type!='MESH' or not o.name.startswith('Spirit_Hood_'):continue
    world=o.matrix_world;inv=world.inverted()
    for v in o.data.vertices:
        p=world@v.co
        weight=smooth((p.z-1.72)/.34)
        if not weight:continue
        # Turn the existing side curl partly toward +Y (the character's back),
        # then gently lean the upper crown back without moving the brim.
        turn=math.radians(42)*weight
        x=p.x*math.cos(turn)-p.y*math.sin(turn)
        y=p.x*math.sin(turn)+p.y*math.cos(turn)
        lean=math.radians(14)*weight;z=p.z-1.72
        p=Vector((x,y*math.cos(lean)+z*math.sin(lean),1.72+z*math.cos(lean)-y*math.sin(lean)))
        v.co=inv@p;changed+=1
(OUT/'hat-check.json').write_text(json.dumps({'deformed_vertices':changed,'brim_below_z_1_72_unchanged':True,'back_axis':'+Y','unity_applied':False},indent=2),encoding='utf-8')
render=base[base.index("root.name='TurnaroundPaperSpirit'"):]
exec(compile(render,str(ROOT/'Tools/model_turnaround_spirit.py'),'exec'))
