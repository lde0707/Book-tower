"""Build a 60 Hz, eight-second looping flight performance on the actual V3 bird.
The existing game's paper-spirit OBJ is used as a scale/companionship reference.
"""
import bpy, math, json
from pathlib import Path
from mathutils import Vector, Matrix, Quaternion
ROOT=Path(__file__).resolve().parents[1]
SRC=ROOT/'ArtSource/PaperMoaV3';OUT=ROOT/'Docs/ArtReview/MoaModelViewsV1/FlightV1'
OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(SRC/'Moa_SculptV3.blend'))
scene=bpy.context.scene;scene.frame_set(1)
rig=bpy.data.objects['Moa_Character']
bird=[rig]+list(rig.children_recursive)
for o in bird:o.animation_data_clear()
rig.location=(0,0,0);rig.rotation_euler=(0,0,0)
bpy.context.view_layer.update()

# A deforming neck/head prevents the eyes and beak moving independently of the skin.
data=bpy.data.armatures.new('Moa flight skeleton');arm=bpy.data.objects.new('Moa_FlightSkeleton',data);scene.collection.objects.link(arm);arm.parent=rig
bpy.context.view_layer.objects.active=arm;arm.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
bodyBone=data.edit_bones.new('Body');bodyBone.head=(0,.015,.352);bodyBone.tail=(0,.015,.55)
headBone=data.edit_bones.new('Head');headBone.head=(0,-.037,.585);headBone.tail=(0,-.037,.84);headBone.parent=bodyBone
bpy.ops.object.mode_set(mode='OBJECT')
def skin(obj,head_only=False):
    if obj.type=='CURVE':
        bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj;bpy.ops.object.convert(target='MESH');obj=bpy.context.object
    if obj.type!='MESH':return
    gb=obj.vertex_groups.new(name='Body');gh=obj.vertex_groups.new(name='Head')
    for v in obj.data.vertices:
        z=(obj.matrix_world@v.co).z
        u=max(0,min(1,(z-.46)/.22));w=1 if head_only else u*u*(3-2*u)
        if w<1:gb.add([v.index],1-w,'REPLACE')
        if w>0:gh.add([v.index],w,'REPLACE')
    mod=obj.modifiers.new('Moa soft neck animation','ARMATURE');mod.object=arm;mod.use_deform_preserve_volume=True
    bpy.context.view_layer.objects.active=obj
    while obj.modifiers.find(mod.name)>0:bpy.ops.object.modifier_move_up(modifier=mod.name)
skin(bpy.data.objects['Moa_SculptedBody'])
for o in list(rig.children_recursive):
    if o.name.startswith(('Moa_Eye','Moa_soft_upper_lid','Moa_Beak','Moa_tiny_mouth_seam','Moa_crown')):skin(o,True)

wings={-1:bpy.data.objects['Moa_Wing_L'],1:bpy.data.objects['Moa_Wing_R']}
feet=[bpy.data.objects['Moa_Foot_-1'],bpy.data.objects['Moa_Foot_1']]
footbase=[o.location.copy() for o in feet]
tail=bpy.data.objects.new('Moa_TailPivot',None);scene.collection.objects.link(tail);tail.parent=rig;tail.location=(0,.255,.276)
bpy.context.view_layer.update()
for o in list(rig.children):
    if o.name.startswith('Moa_tail_'):
        matrix=o.matrix_world.copy();o.parent=tail;o.matrix_world=matrix

tips=[]
for side,wing in wings.items():
    for o in wing.children:
        if o.name.startswith('Moa_primary'):
            o.shape_key_add(name='Basis');key=o.shape_key_add(name='Paper tip follow through')
            for v in key.data:
                u=max(0,min(1,(v.co.y-.17)/.19));v.co.x+=side*.018*u*u;v.co.z+=.012*u*u
            tips.append((side,int(o.name.rsplit('_',1)[1]),key))

# Import the real player model; it is a staging reference, not part of the bird FBX.
before=set(bpy.data.objects)
bpy.ops.wm.obj_import(filepath=str(ROOT/'Assets/_BookTower/Art/PaperSpirit/paper-spirit.obj'),forward_axis='Z',up_axis='Y')
spiritparts=[o for o in bpy.data.objects if o not in before]
player=bpy.data.objects.new('Player_scale_reference',None);scene.collection.objects.link(player)
paper=bpy.data.materials['Moa / warm ivory mulberry paper'];eye=bpy.data.materials['Moa / deep brown lively eyes']
red=paper.copy();red.name='Spirit crimson scarf';p=red.node_tree.nodes.get('Principled BSDF')
for link in list(p.inputs['Base Color'].links):red.node_tree.links.remove(link)
p.inputs['Base Color'].default_value=(.37,.070,.035,1)
for o in spiritparts:
    o.parent=player
    for i,m in enumerate(o.data.materials):
        o.data.materials[i]=red if m and 'Ribbon' in m.name else eye if m and 'Ink' in m.name else paper
# OBJ importer maps Unity Y-up / +Z front to Blender Z-up / -Y front.
bpy.context.view_layer.update()
low=min((o.matrix_world@Vector(c)).z for o in spiritparts for c in o.bound_box)
for o in spiritparts:o.location.z-=low

follow=bpy.data.objects.new('Preview_follow_path',None);scene.collection.objects.link(follow)
rig.parent=follow;rig.scale=(.63,.63,.63)
fps=60;seconds=8;N=fps*seconds
scene.render.fps=fps;scene.frame_start=1;scene.frame_end=N
head=arm.pose.bones['Head'];head.rotation_mode='QUATERNION';basis=head.bone.matrix_local.to_quaternion()
def envelope(t,a,b):
    return math.sin(math.pi*(t-a)/(b-a))**2 if a<t<b else 0
def key(obj,path,frame):obj.keyframe_insert(path,frame=frame)
def pose(t,frame):
    theta=math.tau*t/seconds;phase=math.tau*(3*t+.20*math.sin(theta))
    chase=envelope(t,1.9,5.2);glide=envelope(t,4.1,5.9)
    player.location=(-.52+.16*math.sin(theta),.10*math.cos(theta),.065+.018*math.sin(theta*2));key(player,'location',frame)
    player.rotation_euler=(0,.025*math.sin(theta*2),math.pi-.075*math.sin(theta));key(player,'rotation_euler',frame)
    follow.location=(player.location.x+.93+.14*math.cos(theta),player.location.y-.06+.24*math.sin(theta),.61+.068*math.sin(theta*2));key(follow,'location',frame)
    rig.location=(.008*math.sin(theta*2),.007*math.cos(theta*2),.021*math.cos(phase-.55)+.013*math.sin(theta*3));key(rig,'location',frame)
    rig.rotation_euler=(math.radians(3-11*chase+3*math.sin(theta*2)),math.radians(7)*math.sin(theta),math.radians(-8+17*math.sin(theta)));key(rig,'rotation_euler',frame)
    world=Quaternion((0,0,1),math.radians(-10)*(1+math.cos(theta))*.5)@Quaternion((1,0,0),math.radians(4)*math.sin(theta*2))
    head.rotation_quaternion=basis.inverted()@world@basis;key(head,'rotation_quaternion',frame)
    for side,wing in wings.items():
        beat=math.sin(phase+side*.055)+.12*math.sin(2*phase)
        # Stronger quick downstroke, held open during the short glide.
        angle=72+(41-29*glide+5*chase)*beat
        wing.rotation_euler=(math.radians(5)*math.cos(phase-.3),-side*math.radians(angle),side*math.radians(7)*math.sin(phase-.65));key(wing,'rotation_euler',frame)
    for i,foot in enumerate(feet):
        foot.location=footbase[i]+Vector((0,.004*math.sin(theta+i),.030+.008*math.sin(theta*2+i)))
        foot.rotation_euler=(math.radians(48+5*math.sin(theta*2+i)),0,math.radians(4)*math.sin(theta+i));key(foot,'location',frame);key(foot,'rotation_euler',frame)
    tail.rotation_euler=(math.radians(5)*math.sin(phase-.9),math.radians(3)*math.sin(theta),-rig.rotation_euler.z*.35);key(tail,'rotation_euler',frame)
    for side,k,shape in tips:shape.value=.5+.45*math.sin(phase-.35-k*.14);shape.keyframe_insert('value',frame=frame)
for f in range(1,N+2):pose((f-1)/fps,f)
scene.frame_set(1)

# A camera that includes the actual player's full silhouette and the companion.
cam=scene.camera;cam.location=(-2.3,-5.2,2.05);target=Vector((.0,0,.78));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=2.43
scene.render.resolution_x=960;scene.render.resolution_y=800;scene.render.resolution_percentage=100
scene.cycles.samples=40;scene.cycles.use_denoising=True
try:
    prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='OPTIX';prefs.get_devices()
    for d in prefs.devices:d.use=d.type!='CPU'
    scene.cycles.device='GPU'
except Exception:pass

# Export a local-space animation: the review player's path never goes into FBX.
rig.parent=None;rig.scale=(1,1,1);scene.frame_set(1)
bpy.ops.object.select_all(action='DESELECT')
for o in [rig]+list(rig.children_recursive):o.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(SRC/'Moa_FlightV1.fbx'),use_selection=True,object_types={'MESH','ARMATURE','EMPTY','OTHER'},bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_step=1,bake_anim_simplify_factor=0,add_leaf_bones=False,axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=True)
rig.parent=follow;rig.scale=(.63,.63,.63);scene.frame_set(1)
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'Moa_FlightV1.blend'))
for f,name in [(1,'hover'),(7,'wing-up'),(178,'follow'),(307,'glide')]:
    scene.frame_set(f);scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
scene.frame_set(1)
report={'seconds':seconds,'authored_fps':fps,'loop_start':1,'loop_end_inclusive':N+1,'player_reference':'existing PaperSpirit OBJ','animation':'hover, banked follow, glide, paper tip lag, tucked feet, soft head turn','unity_scene_changed':False,'root_path_exported':False}
(OUT/'animation-check.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('MOA_FLIGHT_READY',flush=True)
