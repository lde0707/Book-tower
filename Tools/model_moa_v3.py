"""Actual Blender model and Cycles review renders. Run with Blender --background --python.

Keep the game scene untouched while the user reviews the new bird's proportions.
Blender Z is up, -Y is the bird's face. Dimensions are approximately metres.
"""
import bpy, math, random, json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'ArtSource/PaperMoaV3'
OUT=ROOT/'Docs/ArtReview/MoaModelViewsV1/SculptV3'
SOURCE.mkdir(parents=True,exist_ok=True); OUT.mkdir(parents=True,exist_ok=True)
random.seed(37)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
for data in bpy.data.materials: bpy.data.materials.remove(data)
scene=bpy.context.scene
scene.render.engine='CYCLES';scene.cycles.samples=72;scene.cycles.use_denoising=True
scene.render.resolution_x=1200;scene.render.resolution_y=1200;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX'
scene.view_settings.look='AgX - Medium High Contrast'
scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.53,.58,.65,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.32
try:
    prefs=bpy.context.preferences.addons['cycles'].preferences
    prefs.compute_device_type='OPTIX';prefs.get_devices()
    gpu=False
    for d in prefs.devices:
        d.use=d.type!='CPU';gpu|=d.use
    if gpu:scene.cycles.device='GPU'
except Exception as e:print('CPU renderer:',e)

def mat(name,color,rough=.7,paper=False):
    m=bpy.data.materials.new(name);m.use_nodes=True;m.diffuse_color=(*color,1)
    nodes=m.node_tree.nodes;links=m.node_tree.links;p=nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=rough
    p.inputs['Specular IOR Level'].default_value=.25
    if paper:
        tex=nodes.new('ShaderNodeTexNoise');tex.inputs['Scale'].default_value=165;tex.inputs['Detail'].default_value=2.5;tex.inputs['Roughness'].default_value=.7
        coords=nodes.new('ShaderNodeTexCoord');links.new(coords.outputs['Object'],tex.inputs['Vector'])
        ramp=nodes.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].position=.12;ramp.color_ramp.elements[1].position=.87
        ramp.color_ramp.elements[0].color=tuple(c*.90 for c in color)+(1,);ramp.color_ramp.elements[1].color=tuple(min(1,c*1.025) for c in color)+(1,)
        links.new(tex.outputs['Fac'],ramp.inputs[0]);links.new(ramp.outputs['Color'],p.inputs['Base Color'])
        fibre=nodes.new('ShaderNodeTexImage');fibre.image=bpy.data.images.load(str(ROOT/'Assets/_BookTower/Art/PaperSpirit/PaperFiber.png'),check_existing=True)
        fibre.projection='BOX';fibre.projection_blend=.32
        mapping=nodes.new('ShaderNodeVectorMath');mapping.operation='SCALE';mapping.inputs[3].default_value=1.15
        links.new(coords.outputs['Object'],mapping.inputs[0]);links.new(mapping.outputs[0],fibre.inputs['Vector'])
        mix=nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=.30
        links.new(ramp.outputs['Color'],mix.inputs[1]);links.new(fibre.outputs['Color'],mix.inputs[2]);links.new(mix.outputs[0],p.inputs['Base Color'])
        bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.40;bump.inputs['Distance'].default_value=.009
        links.new(fibre.outputs['Color'],bump.inputs['Height'])
        fine=nodes.new('ShaderNodeBump');fine.inputs['Strength'].default_value=.24;fine.inputs['Distance'].default_value=.0018
        links.new(tex.outputs['Fac'],fine.inputs['Height']);links.new(bump.outputs['Normal'],fine.inputs['Normal']);links.new(fine.outputs['Normal'],p.inputs['Normal'])
        p.inputs['Subsurface Weight'].default_value=.045
        p.inputs['Subsurface Radius'].default_value=(.9,.5,.25)
    return m

ivory=mat('Moa / warm ivory mulberry paper',(.85,.785,.66),.79,True)
wingmat=mat('Moa / warm coverts',(.78,.705,.585),.77,True)
edge=mat('Moa / feather tips become thin paper',(.83,.765,.64),.86,True)
beakmat=mat('Moa / softly worn ochre beak',(.53,.255,.070),.52,True)
footmat=mat('Moa / small ochre feet',(.41,.204,.077),.64,True)
eyeMat=mat('Moa / deep brown lively eyes',(.016,.009,.004),.19)
eyeMat.node_tree.nodes['Principled BSDF'].inputs['Specular IOR Level'].default_value=.40
mouthmat=mat('Moa / beak seam',(.082,.028,.010),.84)

character=[]
def smooth(obj):
    for p in obj.data.polygons:p.use_smooth=True
def uvball(name,loc,scale,material,seg=64,rings=40):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg,ring_count=rings,location=loc)
    o=bpy.context.object;o.name=name;o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);smooth(o)
    o.data.materials.append(material);character.append(o);return o
def mesh(name,verts,faces,material,sub=1):
    data=bpy.data.meshes.new(name);data.from_pydata(verts,[],faces);data.update()
    o=bpy.data.objects.new(name,data);scene.collection.objects.link(o);o.data.materials.append(material);smooth(o);character.append(o)
    if sub:
        mod=o.modifiers.new('Soft sculpted surface','SUBSURF');mod.levels=sub;mod.render_levels=sub
    return o
def curve(name,points,rad,material,radii=None):
    data=bpy.data.curves.new(name,'CURVE');data.dimensions='3D';data.resolution_u=3
    sp=data.splines.new('BEZIER');sp.bezier_points.add(len(points)-1)
    for i,(b,p) in enumerate(zip(sp.bezier_points,points)):
        b.co=p;b.handle_left_type='AUTO';b.handle_right_type='AUTO';b.radius=radii[i] if radii else 1
    data.bevel_depth=rad;data.bevel_resolution=3;data.resolution_u=12;data.use_fill_caps=True
    o=bpy.data.objects.new(name,data);scene.collection.objects.link(o);o.data.materials.append(material);character.append(o);return o

# Fuse the masses, then sculpt the union: no visible stacked sphere intersections.
parts=[
    uvball('breast mass',(0,.015,.352),(.309,.345,.280),ivory),
    uvball('head mass',(0,-.037,.64),(.291,.271,.279),ivory),
    uvball('rump',(0,.166,.302),(.254,.208,.189),ivory),
    uvball('cheek L',(-.149,-.147,.551),(.123,.120,.131),ivory),
    uvball('cheek R',(.149,-.147,.551),(.123,.120,.131),ivory)]
bpy.ops.object.select_all(action='DESELECT')
for p in parts:p.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();body=bpy.context.object;body.name='Moa_SculptedBody'
character=[body]
rem=body.modifiers.new('Sculpt union','REMESH');rem.mode='VOXEL';rem.voxel_size=.0065;rem.use_smooth_shade=True
bpy.ops.object.modifier_apply(modifier=rem.name)
sm=body.modifiers.new('Soften throat cheeks and back','SMOOTH');sm.factor=.82;sm.iterations=84;bpy.ops.object.modifier_apply(modifier=sm.name)
sub=body.modifiers.new('Sculpt finishing','SUBSURF');sub.levels=1;sub.render_levels=1
bpy.context.view_layer.update()
deps=bpy.context.evaluated_depsgraph_get();bvh=BVHTree.FromObject(body,deps)

def surface_at(x,z):
    inv=body.matrix_world.inverted();origin=inv@Vector((x,-2,z));direction=inv.to_3x3()@Vector((0,1,0))
    hit,normal,_,_=bvh.ray_cast(origin,direction)
    if hit is None:raise RuntimeError('Eye did not reach head')
    return body.matrix_world@hit,(body.matrix_world.to_3x3()@normal).normalized()

# Bigger calm eyes, embedded in the cheek surface, with real studio catchlights.
for side in [-1,1]:
    point,normal=surface_at(side*.174,.659)
    eye=uvball('Moa_Eye_'+str(side),point+normal*.002,(.045,.054,.024),eyeMat)
    eye.rotation_mode='QUATERNION';eye.rotation_quaternion=Vector((0,0,1)).rotation_difference(normal)
    right=eye.rotation_quaternion@Vector((1,0,0));up=eye.rotation_quaternion@Vector((0,1,0))
    pts=[]
    for i in range(13):
        a=.12+math.pi*.93*i/12
        pts.append(point+right*(.047*math.cos(a))+up*(.055*math.sin(a))+normal*.001)
    curve('Moa_soft_upper_lid_'+str(side),pts,.0027,ivory,[.1]+[1]*11+[.1])

def beak(name,base,wide,length,height,material):
    verts=[];faces=[];rows=28;cols=40
    for j in range(rows+1):
        t=j/rows;w=wide*max(0,1-t)**.70;h=height*max(0,1-t)**.8
        for i in range(cols):
            a=i*math.tau/cols;zz=math.sin(a)
            verts.append((w*math.cos(a),base[1]-length*t,base[2]-.019*t+h*zz*(1 if zz>0 else .18)))
            if j<rows:
                q=j*cols+i;n=j*cols+(i+1)%cols;faces.append((q,n,n+cols,q+cols))
    faces.append(tuple(reversed(range(cols))))
    return mesh(name,verts,faces,material,1)
beak('Moa_BeakUpper',(0,-.289,.596),.049,.098,.026,beakmat)
beak('Moa_BeakLower',(0,-.296,.588),.043,.083,.011,beakmat)
curve('Moa_tiny_mouth_seam',[(-.031,-.317,.588),(0,-.365,.578),(.031,-.317,.588)],.0018,mouthmat,[.15,1,.15])

def leaf(name,start,end,width,thick,material,side=1,bulge=.03,curl=.013,ragged=False):
    start=Vector(start);end=Vector(end);axis=end-start;across=Vector((0,-axis.z,axis.y)).normalized()
    # For tails/crest use an across vector along X.
    if abs(axis.x)<.03 and abs(start.x)<.15:across=Vector((1,0,0))
    verts=[];faces=[];rows=42;cols=18
    for j in range(rows+1):
        t=j/rows
        # Rounded broad tip ending in a tiny point; no repeated flat oval plates.
        w=width*(math.sin(math.pi*t)**.60 if 0<t<1 else .004)*(1-.19*t)
        for i in range(cols+1):
            s=-1+2*i/cols;p=start+axis*t+across*w*s
            p.x+=side*(bulge*math.sin(math.pi*t)*(1-s*s)+curl*t**7)
            if abs(start.x)<.15:p.z+=.012*math.sin(math.pi*t)*(1-abs(s))
            if ragged:
                p+=across*(s*.0011*abs(s)**7*(math.sin(t*133)+.40*math.sin(t*241)))
                p.z+=.0013*abs(s)**6*math.sin(t*77)
            verts.append(tuple(p))
            if j<rows and i<cols:
                q=j*(cols+1)+i;faces.append((q,q+1,q+cols+2,q+cols+1))
    o=mesh(name,verts,faces,material,1)
    o.data.materials.append(edge)
    if ragged:
        for poly in o.data.polygons:
            if poly.index//cols>rows*.66:poly.material_index=1
    solid=o.modifiers.new('Physical fine paper edge','SOLIDIFY');solid.thickness=thick;solid.offset=0
    return o

wings={}
for side in [-1,1]:
    root=bpy.data.objects.new('Moa_Wing_'+('L' if side<0 else 'R'),None);scene.collection.objects.link(root);root.location=(side*.251,.005,.478)
    wing_parts=[]
    # Soft wing root is submerged in the back; four feather tips emerge from it.
    covert=uvball('Moa_covert_'+str(side),(side*.261,.109,.378),(.047,.153,.104),wingmat)
    covert.rotation_euler[0]=-.48;wing_parts.append(covert)
    for k in range(4):
        start=(side*(.268+.006*k),.018+.022*k,.459-.020*k)
        end=(side*(.257+.008*k),.322+.014*k,.299-.026*k)
        feather=leaf('Moa_primary_%s_%s'%(side,k),start,end,.076-.006*k,.0021,wingmat,side,bulge=.025-.003*k,curl=.012,ragged=True)
        wing_parts.append(feather)
        # Sparse fibres at the free edge, thinner than a pixel at gameplay distance.
        for n in range(8):
            t=.78+n*.024;ss=(-1 if n%2 else 1);ax=Vector(end)-Vector(start);cross=Vector((0,-ax.z,ax.y)).normalized()
            w=(.076-.006*k)*math.sin(math.pi*t)**.60*(1-.19*t)
            p=Vector(start)+ax*t+cross*w*ss+Vector((side*.012*t**7,0,0))
            strand=curve('Moa_tip_fibre', [p,p+cross*ss*.002+Vector((side*.001,0,.001)),p+cross*ss*.004+Vector((side*.002,0,.0005))],.00028,edge,[.8,.6,.05]);wing_parts.append(strand)
    bpy.context.view_layer.update()
    for o in wing_parts:
        matrix=o.matrix_world.copy();o.parent=root;o.matrix_world=matrix
    wings[side]=root

for n in [-1,0,1]:
    leaf('Moa_tail_'+str(n),(n*.026,.255,.276),(n*.086,.501+(n==0)*.022,.416+(n==0)*.017),.065,.0025,wingmat,side=n or 1,bulge=.005,curl=.003,ragged=True)
# Three short tucked crown feathers; restrained silhouette detail, not a paper hat.
for n in [-1,0,1]:
    leaf('Moa_crown_'+str(n),(n*.035,-.025,.886),(n*.022,.061,.927+(n==0)*.016),.027,.0018,ivory,side=n or 1,bulge=.003,curl=.001,ragged=False)

feet=[]
for side in [-1,1]:
    x=side*.116
    root=bpy.data.objects.new('Moa_Foot_'+str(side),None);scene.collection.objects.link(root);root.location=(x,-.039,.105)
    footobjs=[]
    footobjs.append(curve('Moa_ankle_'+str(side),[(x,.006,.153),(x+side*.006,-.009,.082),(x,-.043,.030)],.012,footmat,[.95,.80,.63]))
    for k in [-1,0,1]:
        end=(x+k*.045,-.111+(abs(k)*.015),.014)
        footobjs.append(curve('Moa_toe_'+str(side)+'_'+str(k),[(x,-.043,.026),(x+k*.027,-.076,.022),end],.010,footmat,[.85,.60,.20]))
    footobjs.append(curve('Moa_back_toe_'+str(side),[(x,-.031,.025),(x+side*.011,.004,.020),(x+side*.012,.032,.013)],.0085,footmat,[.8,.52,.18]))
    bpy.context.view_layer.update()
    for o in footobjs:
        matrix=o.matrix_world.copy();o.parent=root;o.matrix_world=matrix
    feet.append(root)

# Simple review animation, keeping the polished model and sculpted proportions.
rig=bpy.data.objects.new('Moa_Character',None);scene.collection.objects.link(rig)
for o in list(character)+list(wings.values())+feet:
    if o.parent is None:
        matrix=o.matrix_world.copy();o.parent=rig;o.matrix_world=matrix
scene.render.fps=30;scene.frame_start=1;scene.frame_end=90
for f in range(1,91):
    t=(f-1)/90;wave=math.sin(t*math.tau)
    rig.location.z=.004*(1-math.cos(t*math.tau));rig.rotation_euler.z=math.radians(1.2)*wave
    rig.keyframe_insert('location',frame=f);rig.keyframe_insert('rotation_euler',frame=f)
    for side,root in wings.items():
        envelope=math.sin(math.pi*max(0,min(1,(t-.25)/.6)))**2 if .25<t<.85 else 0
        root.rotation_euler.y=-side*math.radians(62)*envelope*(.6+.4*math.sin((t-.25)/.6*math.tau*3))
        root.keyframe_insert('rotation_euler',frame=f)
scene.frame_set(1)

# Studio lighting and contact shadow are part of the actual 3D render.
groundmat=mat('Studio / warm matte',(.35,.30,.24),.87)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,.004));ground=bpy.context.object;ground.name='Studio_floor';ground.data.materials.append(groundmat)
def area(name,loc,power,color,size,target=(0,0,.47)):
    data=bpy.data.lights.new(name,'AREA');data.energy=power;data.color=color;data.shape='DISK';data.size=size
    o=bpy.data.objects.new(name,data);scene.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
area('Large soft window',(-2.5,-3.1,3.4),340,(1,.90,.77),3.0)
area('Cream fill',(2.1,-1.6,1.8),115,(.88,.93,1),2.5)
area('Paper edge rim',(1.4,1.8,2.3),260,(1,.79,.54),1.8)
camData=bpy.data.cameras.new('Moa review');cam=bpy.data.objects.new('Moa review',camData);scene.collection.objects.link(cam);scene.camera=cam
camData.type='ORTHO';camData.ortho_scale=1.23;camData.lens=65
def view(pos,path):
    cam.location=pos;cam.rotation_euler=(Vector((0,.04,.47))-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(path);bpy.ops.render.render(write_still=True)

bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'Moa_SculptV3.blend'))
# Export native geometry separately from the stage. The .blend contains the full
# procedural paper shading; exported interchange meshes are for integration work.
bpy.ops.object.select_all(action='DESELECT')
for o in character:
    if o.name in bpy.data.objects:o.select_set(True)
for o in list(wings.values())+feet+[rig]:o.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(SOURCE/'Moa_SculptV3.fbx'),use_selection=True,object_types={'MESH','EMPTY','OTHER'},apply_unit_scale=True,bake_anim=False,add_leaf_bones=False,axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=True)
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'Moa_SculptV3.blend'))
view((-2.4,-3.5,1.65),OUT/'three-quarter.png')
view((0,-4,1.0),OUT/'front.png')
view((-4,-.20,1.05),OUT/'left.png')
scene.frame_set(45);view((-2.4,-3.5,1.65),OUT/'flutter.png');scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'Moa_SculptV3.blend'))
report={'blender':bpy.app.version_string,'model_objects':len(character),'scene_game_changed':False,'source':'ArtSource/PaperMoaV3/Moa_SculptV3.blend','render':'Cycles actual geometry; studio lighting, not Unity gameplay','meshes':{o.name:len(o.data.polygons) for o in character if o.type=='MESH'}}
(OUT/'model-check.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('MOA_SCULPT_V3_COMPLETE',flush=True)
