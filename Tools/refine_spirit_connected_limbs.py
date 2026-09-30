"""Continuous paper limbs inside sleeves and ankle-length robe; standalone V5."""
import bpy, math, sys, json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
base=(ROOT/'Tools/model_turnaround_spirit.py').read_text(encoding='utf-8').replace('TurnaroundSpiritModelV1','TurnaroundSpiritModelV5')
stage=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else 'model'
if stage=='bake':
    exec(compile(base.split('bpy.ops.wm.open_mainfile')[0],str(ROOT/'Tools/model_turnaround_spirit.py'),'exec'))
    raise SystemExit(0)
SRC=ROOT/'ArtSource/TurnaroundSpiritModelV5';OUT=ROOT/'Docs/ArtReview/TurnaroundSpiritModelV5'
SRC.mkdir(parents=True,exist_ok=True);OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/TurnaroundSpiritModelV4/PaperHoodSpirit.blend'))
scene=bpy.context.scene;root=bpy.data.objects['PaperHoodSpirit']
prefs=bpy.context.preferences.addons['cycles'].preferences
prefs.compute_device_type='OPTIX';prefs.get_devices()
for device in prefs.devices:device.use=device.type!='CPU'
scene.cycles.device='GPU'
mat=bpy.data.materials['Soft ivory face']

def capsule(name,start,end,radius):
    start,end=Vector(start),Vector(end)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=40,ring_count=24,location=(start+end)/2)
    o=bpy.context.object;o.name=name
    o.scale=(radius,radius,(end-start).length/2+radius)
    o.rotation_euler=(end-start).to_track_quat('Z','Y').to_euler()
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return o

checks=[]
def fuse(name,objects):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.object.join();o=bpy.context.object;o.name=name
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    remesh=o.modifiers.new('Continuous paper limb','REMESH');remesh.mode='VOXEL';remesh.voxel_size=.003
    bpy.ops.object.modifier_apply(modifier=remesh.name)
    smooth=o.modifiers.new('Soft wrist and ankle transitions','SMOOTH');smooth.factor=.8;smooth.iterations=5
    bpy.ops.object.modifier_apply(modifier=smooth.name)
    o.data.materials.clear();o.data.materials.append(mat)
    for p in o.data.polygons:p.use_smooth=True
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(island_margin=.025)
    bpy.ops.object.mode_set(mode='OBJECT');o.parent=root
    # Verify that this is one connected surface, not overlapping separate balls.
    adjacency=[[] for _ in o.data.vertices]
    for edge in o.data.edges:
        a,b=edge.vertices;adjacency[a].append(b);adjacency[b].append(a)
    remaining=set(range(len(adjacency)));components=0
    while remaining:
        components+=1;stack=[remaining.pop()]
        while stack:
            for v in adjacency[stack.pop()]:
                if v in remaining:remaining.remove(v);stack.append(v)
    assert components==1,(name,components)
    checks.append({'name':name,'connected_components':components,'vertices':len(o.data.vertices)})
    return o

for side in (-1,1):
    ankle=bpy.data.objects.get('Spirit_Ankle_'+str(side))
    if ankle:bpy.data.objects.remove(ankle,do_unlink=True)
    foot=bpy.data.objects['Spirit_Foot_'+str(side)]
    leg=capsule('Leg core',(side*.12,-.025,.078),(side*.12,.0,.46),.058)
    fuse('Spirit_LegFoot_'+str(side),[foot,leg])
    hand=bpy.data.objects['Spirit_Mitten_'+str(side)]
    arm=capsule('Arm inside sleeve',(side*.145,-.008,.947),(side*.407,-.093,.785),.047)
    wrist=capsule('Wrist',(side*.393,-.087,.803),(side*.438,-.096,.756),.047)
    fuse('Spirit_ArmHand_'+str(side),[hand,arm,wrist])

# Stretch each lower panel and its attached folds/fibres by the same transform.
# Minimum hem height is 6 cm; the feet are about 11 cm high.
for prefix in ('Spirit_Cape_lower_left','Spirit_Cape_lower_right','Spirit_Cape_back_long'):
    group=[o for o in root.children if o.type=='MESH' and o.name.startswith(prefix)]
    minimum=min(v.co.z for o in group for v in o.data.vertices)
    pivot=.80;factor=(pivot-.060)/(pivot-minimum)
    for o in group:
        for v in o.data.vertices:
            if v.co.z<pivot:v.co.z=pivot+(v.co.z-pivot)*factor

# A continuous paper underskirt covers the raised gaps between the torn outer hems.
# Its narrow silhouette sits inside the layered mantle; only the toes remain visible.
vertices=[];faces=[];uvs=[];nu=112;nv=28
for j in range(nv+1):
    t=j/nv
    for i in range(nu+1):
        u=i/nu;a=math.tau*u
        r=.22+.060*t+.007*math.sin(7*a+.6*t)+.003*math.sin(13*a)*t
        hem=.006*math.sin(11*a)+.003*math.sin(31*a)
        vertices.append((r*math.sin(a),-r*.84*math.cos(a),.68-.607*t+hem*t**8))
        uvs.append((u*2,t))
for j in range(nv):
    for i in range(nu):
        k=j*(nu+1)+i;faces.append((k,k+1,k+nu+2,k+nu+1))
data=bpy.data.meshes.new('Long continuous inner paper robe');data.from_pydata(vertices,[],faces);data.update()
skirt=bpy.data.objects.new('Spirit_Cape_inner_ankle_length',data);scene.collection.objects.link(skirt);skirt.parent=root
data.materials.append(bpy.data.materials['Soft fibrous garment paper'])
uv=data.uv_layers.new(name='UVMap')
for p in data.polygons:
    p.use_smooth=True
    for li in p.loop_indices:uv.data[li].uv=uvs[data.loops[li].vertex_index]
solid=skirt.modifiers.new('Thin paper edge','SOLIDIFY');solid.thickness=.002;solid.offset=0

(OUT/'limb-check.json').write_text(json.dumps({'limbs':checks,'minimum_hem_height_m':.06,'unity_applied':False},indent=2),encoding='utf-8')
# Reuse the same studio, camera positions and native-model render/export layout.
render=base[base.index("root.name='TurnaroundPaperSpirit'"):]
exec(compile(render,str(ROOT/'Tools/model_turnaround_spirit.py'),'exec'))
