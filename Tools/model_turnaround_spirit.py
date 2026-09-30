"""Actual model based on the six-view paper spirit sheet; isolated review asset.
Blender --background --python Tools/model_turnaround_spirit.py -- model|bake
"""
import bpy, bmesh, ast, math, sys, json
from pathlib import Path
from mathutils import Vector, noise
ROOT=Path(__file__).resolve().parents[1]
SRC=ROOT/'ArtSource/TurnaroundSpiritModelV1';OUT=ROOT/'Docs/ArtReview/TurnaroundSpiritModelV1'
SRC.mkdir(parents=True,exist_ok=True);OUT.mkdir(parents=True,exist_ok=True)
stage=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else 'model'
if stage=='bake':
    code=(ROOT/'Tools/bake_hood_spirit.py').read_text(encoding='utf-8').replace('ArtSource/PaperHoodSpiritV1','ArtSource/TurnaroundSpiritModelV1').replace('Docs/ArtReview/SpiritRedesignV1/ModelV1','Docs/ArtReview/TurnaroundSpiritModelV1')
    code=code.replace('scene=bpy.context.scene;scene.cycles.samples=8', '''scene=bpy.context.scene;scene.cycles.samples=8
try:
    prefs=bpy.context.preferences.addons['cycles'].preferences
    prefs.compute_device_type='OPTIX';prefs.get_devices()
    for device in prefs.devices:device.use=device.type!='CPU'
    scene.cycles.device='GPU'
except Exception:pass''')
    exec(compile(code,str(ROOT/'Tools/bake_hood_spirit.py'),'exec'));raise SystemExit(0)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/PaperHoodSpiritV5/PaperHoodSpirit.blend'))
scene=bpy.context.scene;root=bpy.data.objects['PaperHoodSpirit'];parts=[]
try:
    prefs=bpy.context.preferences.addons['cycles'].preferences
    prefs.compute_device_type='OPTIX';prefs.get_devices()
    for device in prefs.devices:device.use=device.type!='CPU'
    scene.cycles.device='GPU'
except Exception:pass
paper=bpy.data.materials['Ivory handmade paper'];inside=bpy.data.materials['Warm paper inner layers']
facepaper=bpy.data.materials['Soft ivory face'];red=bpy.data.materials['Terracotta bookmark']
garment=bpy.data.materials['Soft fibrous garment paper']
tree=ast.parse((ROOT/'Tools/model_hood_spirit.py').read_text(encoding='utf-8'))
for node in tree.body:
    if isinstance(node,ast.FunctionDef) and node.name in ('mesh','grid','ball','crinkle'):
        exec(compile(ast.Module(body=[node],type_ignores=[]),'mesh_helpers','exec'))

# The reference has small feet beneath the paper robe rather than a ghost tail.
for o in list(root.children):
    if 'single_curled_tail' in o.name or 'red_bookmark' in o.name or 'Cape_back' in o.name:
        bpy.data.objects.remove(o,do_unlink=True)
face=bpy.data.objects['Spirit_Face'];face.scale=(.87,.92,.89)
for o in root.children:
    if 'Spirit_Eye_' in o.name:
        o.location.x*=.87;o.location.y=-.085+(o.location.y+.085)*.92
        o.location.z=1.315+(o.location.z-1.315)*.89;o.scale*=.85
    if o.type=='MESH' and 'Cape' in o.name:
        for v in o.data.vertices:
            v.co.z=.98-(.98-v.co.z)*1.19
            v.co.z-=.08*max(0,min(1,(.55-v.co.z)/.35))
    if o.name.startswith('Spirit_Scarf_tail_'):
        # Both tails emerge at the same anatomical shoulder in the turnaround.
        for v in o.data.vertices:v.co.x=-abs(v.co.x)
        if o.name.endswith('1') and not o.name.endswith('-1'):
            for v in o.data.vertices:v.co.z-=.075;v.co.y+=.035

for side in (-1,1):
    ball('Spirit_Foot_'+str(side),(side*.12,-.075,.052),(.087,.118,.057),facepaper,48,28)
    ball('Spirit_Ankle_'+str(side),(side*.12,.005,.135),(.06,.06,.095),garment,36,22)
    # Three shallow toe folds, modeled into the foot surface without detached balls.
    foot=bpy.data.objects['Spirit_Foot_'+str(side)]
    for v in foot.data.vertices:
        v.co.z=max(v.co.z,-.052)
        if v.co.y<-.055 and v.co.z>-.025:
            channels=sum(math.exp(-((v.co.x-k)/.006)**2) for k in (-.035,.035))
            v.co.z-=.005*channels*min(1,(-v.co.y-.055)/.06)

# A longer rear paper mantle closes the garment around the lower body.
def back_surface(u,t,lift=0):
    a=.77+4.75*u+.11*math.sin(math.pi*t)
    r=.135+.285*math.sin(t*math.pi*.52)+.020*math.sin(4*a)*t*t
    z=.97-.79*t+.055*max(0,(t-.80)/.2)**2
    z+=.018*math.sin(7*a+.5)*t**5+.004*math.sin(31*a)*t**10
    z-=.08*max(0,min(1,(.55-z)/.35))
    p=Vector(((r+lift)*math.sin(a),-(r+lift)*.81*math.cos(a),z))
    p+=crinkle(p,.0025)*math.sin(math.pi*t)
    return p
grid('Spirit_Cape_back_long_mantle',back_surface,90,44,garment,.002,(2.4,2))
for idx in range(3):
    def back_page(u,t,idx=idx):
        uu=.06+.88*u+.012*math.sin(math.pi*t)
        hem=.12*math.sin(u*math.pi+idx*.7)+.075*(u-.5)
        hem+=.006*math.sin(u*73+idx)
        tt=.11+.205*idx+t*(.26+hem)
        return back_surface(uu,tt,.007+.013*t**4+.005*math.sin(u*math.pi)*math.sin(t*math.pi))
    grid('Spirit_Cape_back_long_layer_'+str(idx),back_page,64,24,garment,.0015,(2,1))

# Worn cuffs: notches are cut into the sleeve itself, with sparse attached pulp fibers.
for sleeve in [o for o in root.children if o.name.startswith('Spirit_Sleeve_')]:
    side=-1 if sleeve.name.endswith('-1') else 1
    uv=sleeve.data.uv_layers.active;progress={}
    for poly in sleeve.data.polygons:
        for li in poly.loop_indices:
            vi=sleeve.data.loops[li].vertex_index;progress[vi]=max(progress.get(vi,0),uv.data[li].uv.y)
    boundary=[]
    for v in sleeve.data.vertices:
        t=progress.get(v.index,0)
        if t<.88:continue
        a=math.atan2(v.co.z-.788,v.co.y+.093)
        cut=.002*math.sin(17*a)+.0015*math.sin(29*a)
        for angle in (-2.1,-.3,1.45):
            d=math.atan2(math.sin(a-angle),math.cos(a-angle));cut+=.014*math.exp(-(d/.105)**2)
        v.co.x-=side*cut*((t-.88)/.12)**2
        if t>.999:boundary.append(v.co.copy())
    for idx,p in enumerate(boundary[::5]):
        def strand(u,t,p=p,idx=idx,side=side):
            return p+Vector((side*(.004+.002*(idx%3))*t,.0015*math.sin(idx)*t,(u-.5)*.001*(1-t)))
        grid('Spirit_Sleeve_pulp_'+str(side)+'_'+str(idx),strand,1,3,garment,0,(.04,.1))

for o in parts:o.parent=root
root.name='TurnaroundPaperSpirit'
# Keep the established bake tool's expected source name.
root.name='PaperHoodSpirit'
scene.render.resolution_x=1440;scene.render.resolution_y=1440;scene.cycles.samples=64
ground=bpy.data.objects.get('Studio');ground.location.z=-.008
cam=scene.camera;cam.data.type='ORTHO';cam.data.ortho_scale=2.60
def set_view(pos,target=(0,0,1.13)):
    cam.location=pos;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler()
set_view((2.7,-4.8,2.1))
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'PaperHoodSpirit.blend'))
views=[('01-hero',(2.7,-4.8,2.1)),('02-front',(0,-5,1.13)),('03-left',(-5,0,1.13)),('04-right',(5,0,1.13)),('05-back',(0,5,1.13)),('06-top',(0,0,6)),('07-bottom',(0,0,-5))]
for name,pos in views:
    ground.hide_render=name in ('06-top','07-bottom')
    set_view(pos);scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
ground.hide_render=False
cam.data.ortho_scale=1.38;set_view((1.8,-5,1.85),(0,-.04,1.12))
scene.render.filepath=str(OUT/'08-detail.png');bpy.ops.render.render(write_still=True)
print('TURNAROUND_MODEL_COMPLETE',flush=True)
