"""Bake the approved sculpt, add flight skeleton and export the real Unity companion."""
import bpy, math, json
from pathlib import Path
from mathutils import Vector,Euler
R=Path(__file__).resolve().parents[1];D=R/'Assets/_BookTower/Art/MoaSculpt';D.mkdir(parents=True,exist_ok=True)
O=R/'Docs/ArtReview/MoaModelViewsV1/UnityV3';O.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(R/'ArtSource/PaperMoaV3/Moa_SculptV3.blend'))
s=bpy.context.scene;s.frame_set(1)
root=bpy.data.objects['Moa_Character'];objs=[o for o in root.children_recursive if o.type in {'MESH','CURVE'}]
for o in [root]+list(root.children_recursive):o.animation_data_clear()
def clamp(t):t=max(0,min(1,t));return t*t*(3-2*t)
for o in objs:
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
    bpy.ops.object.convert(target='MESH');o=bpy.context.object
    # Retain the approved silhouette but reduce dense subdivided sculpt interiors.
    if len(o.data.polygons)>1800:
        m=o.modifiers.new('Game sculpt reduction','DECIMATE');m.ratio=.27;bpy.ops.object.modifier_apply(modifier=m.name)
    name=o.name
    for v in o.data.vertices:
        p=o.matrix_world@v.co
        if any(k in name for k in ['primary','covert','tip_fibre']):w={'WingL' if p.x<0 else 'WingR':1}
        elif 'tail' in name:w={'Tail':1}
        elif any(k in name for k in ['ankle','toe']):w={'FootL' if p.x<0 else 'FootR':1}
        elif any(k in name for k in ['Eye','lid','Beak','mouth','crown']):w={'Head':1}
        else:
            h=clamp((p.z-.46)/.22);w={'Body':1-h,'Head':h}
        for n,value in w.items():
            if value>0:
                g=o.vertex_groups.get(n) or o.vertex_groups.new(name=n);g.add([v.index],value,'REPLACE')
    mat=o.matrix_world.copy();o.parent=None;o.matrix_world=mat
bpy.ops.object.select_all(action='DESELECT')
for o in objs:o.select_set(True)
bpy.context.view_layer.objects.active=objs[0];bpy.ops.object.join();body=bpy.context.object;body.name='Moa_SculptSkin'
bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
for uv in list(body.data.uv_layers):body.data.uv_layers.remove(uv)
body.data.uv_layers.new(name='Atlas')
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=math.radians(70),island_margin=.008);bpy.ops.object.mode_set(mode='OBJECT')
s.cycles.samples=8;s.render.bake.margin=12;s.render.bake.use_pass_direct=False;s.render.bake.use_pass_indirect=False;s.render.bake.use_pass_color=True
images={}
for name,typ in [('BaseColor','DIFFUSE'),('Normal','NORMAL'),('Roughness','ROUGHNESS')]:
    im=bpy.data.images.new('Moa_'+name,width=2048,height=2048,alpha=False);im.colorspace_settings.name='sRGB' if name=='BaseColor' else 'Non-Color'
    for mat in body.data.materials:
        n=mat.node_tree.nodes.new('ShaderNodeTexImage');n.image=im;mat.node_tree.nodes.active=n
    bpy.ops.object.bake(type=typ);im.filepath_raw=str(D/('Moa_'+name+'.png'));im.file_format='PNG';im.save();images[name]=im
    print('BAKED',name,flush=True)
mat=bpy.data.materials.new('Moa baked PBR');mat.use_nodes=True;n=mat.node_tree.nodes;l=mat.node_tree.links;p=n.get('Principled BSDF')
for key,slot in [('BaseColor','Base Color'),('Roughness','Roughness')]:
    tx=n.new('ShaderNodeTexImage');tx.image=images[key];l.new(tx.outputs['Color'],p.inputs[slot])
tx=n.new('ShaderNodeTexImage');tx.image=images['Normal'];nm=n.new('ShaderNodeNormalMap');l.new(tx.outputs['Color'],nm.inputs['Color']);l.new(nm.outputs[0],p.inputs['Normal'])
body.data.materials.clear();body.data.materials.append(mat)
for f in body.data.polygons:f.material_index=0
data=bpy.data.armatures.new('MoaSculptSkeleton');arm=bpy.data.objects.new('MoaRig',data);s.collection.objects.link(arm)
bpy.context.view_layer.objects.active=arm;arm.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
for name,pos,parent in [('Body',(0,0,.35),None),('Head',(0,-.037,.585),'Body'),('WingL',(-.25,.005,.478),'Body'),('WingR',(.25,.005,.478),'Body'),('Tail',(0,.255,.276),'Body'),('FootL',(-.116,0,.12),'Body'),('FootR',(.116,0,.12),'Body')]:
    b=data.edit_bones.new(name);b.head=pos;b.tail=Vector(pos)+Vector((0,0,.10))
    if parent:b.parent=data.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT');mod=body.modifiers.new('Flight skin','ARMATURE');mod.object=arm;body.parent=arm
for pb in arm.pose.bones:pb.rotation_mode='QUATERNION'
basis={b.name:b.bone.matrix_local.to_quaternion() for b in arm.pose.bones}
def rot(n,x=0,y=0,z=0):
    q=Euler(tuple(math.radians(v) for v in (x,y,z))).to_quaternion();b=basis[n];arm.pose.bones[n].rotation_quaternion=b.conjugated()@q@b
s.render.fps=60
for name,duration,cycles in [('Hover',2,6),('Fly',1,4),('Flutter',1.5,7)]:
    arm.animation_data_clear();act=bpy.data.actions.new(name);act.use_fake_user=True;arm.animation_data_create();arm.animation_data.action=act
    for f in range(round(duration*60)+1):
        u=f/(duration*60);phase=math.tau*u;flap=math.sin(phase*cycles)
        rot('Body',x=4+3*math.sin(phase),y=2*math.sin(phase));rot('Head',x=-4-2*math.sin(phase),z=4*math.sin(phase))
        rot('WingL',y=25+38*flap);rot('WingR',y=-(25+38*flap))
        rot('Tail',x=7*math.sin(phase-.5));rot('FootL',x=-55);rot('FootR',x=-55)
        arm.pose.bones['Body'].location=basis['Body'].conjugated()@Vector((0,0,.015*math.sin(phase)))
        for pb in arm.pose.bones:
            pb.keyframe_insert('location',frame=f+1);pb.keyframe_insert('rotation_quaternion',frame=f+1)
    act['loop']=True
arm.animation_data.action=bpy.data.actions['Hover'];s.frame_start=1;s.frame_end=120;s.frame_set(1)
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(R/'ArtSource/PaperMoaV3/Moa_UnityRig.blend'))
bpy.ops.object.select_all(action='DESELECT');arm.select_set(True);body.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(D/'MoaSculpt_Rigged.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y')
px=list(images['Roughness'].pixels)
for i in range(0,len(px),4):px[i:i+4]=[0,0,0,1-px[i]]
im=bpy.data.images.new('Moa_MetallicSmoothness',width=2048,height=2048,alpha=True);im.colorspace_settings.name='Non-Color';im.pixels=px;im.filepath_raw=str(D/'Moa_MetallicSmoothness.png');im.file_format='PNG';im.save()
body.data.calc_loop_triangles();(O/'model-check.json').write_text(json.dumps({'triangles':len(body.data.loop_triangles),'bones':7,'source':'Moa_SculptV3.blend','clips':['Hover','Fly','Flutter']},indent=2))
print('MOA_UNITY_READY',flush=True)
