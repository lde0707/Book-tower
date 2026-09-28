"""Skin the reviewed paper spirit, author 60Hz continuous actions, export Unity FBX."""
import bpy, math, json, shutil
from pathlib import Path
from mathutils import Vector, Euler
from mathutils.kdtree import KDTree
ROOT=Path(__file__).resolve().parents[1];SRC=ROOT/'ArtSource/PaperHoodSpiritV1'
OUT=ROOT/'Docs/ArtReview/SpiritRedesignV1/AnimationV1';OUT.mkdir(parents=True,exist_ok=True)
DEST=ROOT/'Assets/_BookTower/Art/PaperHoodSpirit';DEST.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(SRC/'PaperHoodSpirit.blend'))
scene=bpy.context.scene;root=bpy.data.objects['PaperHoodSpirit']
native=list(root.children);samples=[]
def smooth(v):
    v=max(0,min(1,v));return v*v*(3-2*v)
def chain(p,first,second,top,low):
    w=smooth((top-p.z)/(top-low));return {first:1-w,second:w}
def weights(name,p):
    if 'Eye_' in name:return {'Eye'+('L' if name.endswith('L') else 'R'):1}
    if 'Face' in name or 'edge_fibre' in name:return {'Head':1}
    if 'Hood_curled' in name:
        w=smooth((p.y-.18)/.38);return {'Head':1-w,'HoodTip':w}
    if 'Hood' in name:
        w=.75*smooth((p.y-.22)/.40)
        return {'Head':1-w,'HoodTip':w}
    if 'bookmark' in name:return chain(p,'Body','Bookmark',.98,.81)
    if 'Sleeve' in name or 'Mitten' in name:
        arm='ArmL' if p.x<0 else 'ArmR'
        w=1 if 'Mitten' in name else smooth((abs(p.x)-.15)/.24)
        return {'Body':1-w,arm:w}
    if 'Cape' in name:
        group='CapeBack' if 'back' in name else 'CapeL' if 'left' in name else 'CapeR'
        if p.z>.68:return chain(p,'Body',group,.97,.68)
        return chain(p,group,group+'Tip',.68,.37)
    if 'tail' in name:return chain(p,'Tail','TailTip',.37,.14)
    return chain(p,'Body','Tail',.53,.25)
dg=bpy.context.evaluated_depsgraph_get()
for o in native:
    ev=o.evaluated_get(dg);m=ev.to_mesh()
    for v in m.vertices:
        p=o.matrix_world@v.co;samples.append((p.copy(),weights(o.name,p)))
    ev.to_mesh_clear()
# Use reviewed, already baked geometry so Unity receives exactly the same paper materials.
with bpy.data.libraries.load(str(SRC/'PaperHoodSpirit_Baked.blend'),link=False) as (fr,to):
    to.objects=['PaperHoodSpirit_Baked']
body=to.objects[0];scene.collection.objects.link(body);body.parent=None;body.matrix_world.identity()
for o in native:bpy.data.objects.remove(o,do_unlink=True)
bpy.data.objects.remove(root,do_unlink=True)
body.name='HoodSpirit_Skin'
kd=KDTree(len(samples))
for i,(p,w) in enumerate(samples):kd.insert(p,i)
kd.balance()

bpy.ops.object.select_all(action='DESELECT')
ad=bpy.data.armatures.new('PaperSpiritSkeleton');arm=bpy.data.objects.new('SpiritRig',ad);scene.collection.objects.link(arm)
bpy.context.view_layer.objects.active=arm;arm.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
spec=[('MotionRoot',(0,0,0),None),('Body',(0,0,.80),'MotionRoot'),('Head',(0,0,1.01),'Body'),('HoodTip',(0,.27,1.68),'Head'),('ArmL',(-.16,0,.93),'Body'),('ArmR',(.16,0,.93),'Body'),('Bookmark',(0,-.30,.96),'Body'),('Tail',(0,0,.43),'Body'),('TailTip',(.07,.1,.22),'Tail'),('EyeL',(-.143,-.38,1.325),'Head'),('EyeR',(.143,-.38,1.325),'Head')]
for group,x,y in [('CapeL',-.12,-.03),('CapeR',.12,-.03),('CapeBack',0,.12)]:
    spec.extend([(group,(x,y,.90),'Body'),(group+'Tip',(x*2,y*2,.61),group)])
for name,pos,parent in spec:
    b=ad.edit_bones.new(name);b.head=pos;b.tail=Vector(pos)+Vector((0,0,.12))
    if parent:b.parent=ad.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT')
groups={name:body.vertex_groups.new(name=name) for name,_,_ in spec}
maxerr=0
for v in body.data.vertices:
    _,i,dist=kd.find(v.co);maxerr=max(maxerr,dist)
    w=samples[i][1]
    for name,val in w.items():
        if val>1e-6:groups[name].add([v.index],val,'REPLACE')
assert maxerr<.002,maxerr
mod=body.modifiers.new('Paper deformation skeleton','ARMATURE');mod.object=arm;mod.use_deform_preserve_volume=True
body.parent=arm
for p in arm.pose.bones:p.rotation_mode='QUATERNION'
basis={p.name:p.bone.matrix_local.to_quaternion() for p in arm.pose.bones}
def rot(name,x=0,y=0,z=0):
    q=Euler(tuple(math.radians(a) for a in (x,y,z)),'XYZ').to_quaternion();b=basis[name]
    arm.pose.bones[name].rotation_quaternion=b.conjugated()@q@b
def move(name,x=0,y=0,z=0):arm.pose.bones[name].location=basis[name].conjugated()@Vector((x,y,z))
def blink(t,centers):
    a=0
    for c in centers:
        d=abs(t-c)
        if d<.11:a=max(a,math.cos(d/.11*math.pi/2)**2)
    for name in ['EyeL','EyeR']:arm.pose.bones[name].scale=(1,1-.94*a,1)
clips=[('Idle',4,True),('Move',.9,True),('JumpRise',.6,False),('Fall',1.4,True),('Glide',1.8,True),('Land',.45,False),('LookAround',4.8,False),('TidyBookmark',3.8,False),('Stretch',4.5,False)]
scene.render.fps=60
for name,duration,loop in clips:
    arm.animation_data_clear();action=bpy.data.actions.new(name);action.use_fake_user=True;arm.animation_data_create();arm.animation_data.action=action
    n=round(duration*60)
    for f in range(n+1):
        t=f/60;u=f/n;phase=math.tau*u;env=math.sin(math.pi*u)**2
        for p in arm.pose.bones:p.location=(0,0,0);p.rotation_quaternion=(1,0,0,0);p.scale=(1,1,1)
        sw=math.sin(phase);late=math.sin(phase-.65)
        move('MotionRoot',z=.017*sw);rot('Body',y=1.8*sw);rot('Head',x=1.6*math.sin(phase+.4),z=2*sw)
        rot('ArmL',y=-4-3*late);rot('ArmR',y=4+3*late)
        rot('HoodTip',x=5*late);rot('Tail',x=5*late,y=3*sw);rot('TailTip',x=7*math.sin(phase-1))
        rot('Bookmark',x=4*late)
        for k,g in enumerate(['CapeL','CapeR','CapeBack']):
            rot(g,x=2.5*math.sin(phase-.4*k),y=(-1 if k==0 else 1)*2*sw)
            rot(g+'Tip',x=5*math.sin(phase-.65-.4*k))
        if name=='Move':
            move('MotionRoot',z=.027*math.sin(phase*2));rot('Body',x=12,y=5*sw);rot('Head',x=-7,y=-2*sw)
            rot('ArmL',x=12*sw,y=-12);rot('ArmR',x=-12*sw,y=12)
            rot('HoodTip',x=-12+7*late);rot('Tail',x=-15+8*late,y=7*sw);rot('TailTip',x=-12+10*math.sin(phase-1))
            for k,g in enumerate(['CapeL','CapeR','CapeBack']):rot(g,x=-7+4*late);rot(g+'Tip',x=-12+9*math.sin(phase-.6-k*.3))
            rot('Bookmark',x=-20+6*late)
        elif name=='JumpRise':
            rot('Body',x=-10*(1-u));move('MotionRoot',z=.025*math.sin(math.pi*u))
            rot('ArmL',y=32*math.sin(math.pi*u*.8));rot('ArmR',y=-32*math.sin(math.pi*u*.8))
            rot('Tail',x=24*math.sin(math.pi*u));rot('TailTip',x=20*env)
            rot('Head',x=-8*(1-u));rot('HoodTip',x=14*env)
        elif name in ['Fall','Glide']:
            strength=1 if name=='Fall' else 1.5
            rot('Body',x=-5);rot('Head',x=5);rot('ArmL',y=28*strength+5*sw);rot('ArmR',y=-28*strength-5*sw)
            rot('Tail',x=20+6*late);rot('HoodTip',x=12+5*late)
            for g in ['CapeL','CapeR','CapeBack']:rot(g,x=12+4*late);rot(g+'Tip',x=20+7*sw)
        elif name=='Land':
            pulse=math.sin(math.pi*u)**2;move('MotionRoot',z=-.055*pulse);rot('Body',x=8*pulse);rot('Head',x=-4*pulse)
            rot('Tail',x=-18*pulse);rot('TailTip',x=25*pulse)
            for g in ['CapeL','CapeR','CapeBack']:rot(g+'Tip',x=-15*pulse)
        elif name=='LookAround':
            look=env*math.sin(phase)*28;rot('Head',z=look,x=-6*env);rot('Body',z=look*.20);rot('HoodTip',x=6*late*env)
            rot('ArmR',y=10+9*env)
        elif name=='TidyBookmark':
            rot('Head',x=17*env,z=-5*env);rot('ArmL',x=-29*env,y=34*env,z=60*env);rot('ArmR',x=-29*env,y=-34*env,z=-60*env)
            rot('Bookmark',x=-6*env+4*env*math.sin(phase*3));rot('Body',x=4*env)
        elif name=='Stretch':
            rot('ArmL',y=65*env,x=-10*env);rot('ArmR',y=-65*env,x=-10*env);rot('Head',x=-14*env)
            move('MotionRoot',z=.075*env);rot('Body',x=-7*env);rot('Tail',x=20*env);rot('TailTip',x=-25*env)
        blink(t,[duration*.32,duration*.79] if name!='Move' else [])
        # Every action keys every bone, avoiding stale additive poses when interrupted.
        for p in arm.pose.bones:
            p.keyframe_insert('location',frame=f+1);p.keyframe_insert('rotation_quaternion',frame=f+1);p.keyframe_insert('scale',frame=f+1)
    action['loop']=loop
arm.animation_data.action=bpy.data.actions['Idle'];scene.frame_start=1;scene.frame_end=240;scene.frame_set(1)
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'PaperHoodSpirit_Rigged.blend'))
bpy.ops.object.select_all(action='DESELECT');arm.select_set(True);body.select_set(True);bpy.context.view_layer.objects.active=arm
bpy.ops.export_scene.fbx(filepath=str(DEST/'PaperHoodSpirit_Rigged.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=False)
for p in (SRC/'Textures').glob('*.png'):shutil.copy2(p,DEST/p.name)
# URP metallic map alpha stores smoothness, not roughness.
rough=bpy.data.images.load(str(SRC/'Textures/Spirit_Roughness.png'));px=list(rough.pixels)
for i in range(0,len(px),4):px[i:i+4]=[0,0,0,1-px[i]]
im=bpy.data.images.new('Spirit_MetallicSmoothness',width=2048,height=2048,alpha=True);im.colorspace_settings.name='Non-Color';im.pixels=px;im.filepath_raw=str(DEST/'Spirit_MetallicSmoothness.png');im.file_format='PNG';im.save()
report={'bones':len(spec),'skin_transfer_max_error':maxerr,'clips':[{'name':n,'seconds':d,'loop':l,'fps':60} for n,d,l in clips],'root_motion':False}
(OUT/'rig-check.json').write_text(json.dumps(report,indent=2))
print('SPIRIT_RIG_COMPLETE',json.dumps(report),flush=True)
