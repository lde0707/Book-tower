"""Build an actual paper-hood spirit mesh and studio review. Blender 5.1, Z up, -Y front."""
import bpy, bmesh, math, json, random
from pathlib import Path
from mathutils import Vector, noise

ROOT=Path(__file__).resolve().parents[1]
SRC=ROOT/'ArtSource/PaperHoodSpiritV1'
OUT=ROOT/'Docs/ArtReview/SpiritRedesignV1/ModelV1'
SRC.mkdir(parents=True,exist_ok=True); OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene
scene.render.engine='CYCLES'; scene.cycles.samples=48; scene.cycles.use_denoising=True
scene.render.resolution_x=1200; scene.render.resolution_y=1200; scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX'; scene.view_settings.look='AgX - Medium High Contrast'
scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.62,.65,.70,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.32
try:
    prefs=bpy.context.preferences.addons['cycles'].preferences
    prefs.compute_device_type='OPTIX'; prefs.get_devices()
    for dev in prefs.devices: dev.use=dev.type!='CPU'
    scene.cycles.device='GPU'
except Exception: pass

fiber=bpy.data.images.load(str(ROOT/'Assets/_BookTower/Art/PaperSpirit/PaperFiber.png'))
normal=bpy.data.images.load(str(ROOT/'Assets/_BookTower/Art/PaperSpirit/PaperFiberNormal.png'))
normal.colorspace_settings.name='Non-Color'
parts=[]
def material(name,color,paper=True,rough=.8,detail=.3):
    m=bpy.data.materials.new(name); m.use_nodes=True; m.diffuse_color=(*color,1)
    n=m.node_tree.nodes; l=m.node_tree.links; p=n.get('Principled BSDF')
    p.inputs['Base Color'].default_value=(*color,1); p.inputs['Roughness'].default_value=rough
    p.inputs['Specular IOR Level'].default_value=.26
    if paper:
        # Export-friendly UV textures; the .blend also carries fine wrinkle shading.
        tex=n.new('ShaderNodeTexImage'); tex.image=fiber
        mix=n.new('ShaderNodeMixRGB'); mix.blend_type='MULTIPLY'; mix.inputs[0].default_value=1
        mix.inputs[2].default_value=(*color,1); l.new(tex.outputs['Color'],mix.inputs[1]); l.new(mix.outputs[0],p.inputs['Base Color'])
        tx=n.new('ShaderNodeTexImage'); tx.image=normal
        nm=n.new('ShaderNodeNormalMap'); nm.inputs['Strength'].default_value=detail
        l.new(tx.outputs['Color'],nm.inputs['Color'])
        coord=n.new('ShaderNodeTexCoord')
        vor=n.new('ShaderNodeTexNoise'); vor.inputs['Scale'].default_value=52;vor.inputs['Detail'].default_value=3;vor.inputs['Roughness'].default_value=.72
        l.new(coord.outputs['Object'],vor.inputs['Vector'])
        crease=n.new('ShaderNodeValToRGB');crease.color_ramp.interpolation='EASE'
        crease.color_ramp.elements[0].position=.27;crease.color_ramp.elements[1].position=.74
        l.new(vor.outputs['Fac'],crease.inputs[0])
        bump=n.new('ShaderNodeBump'); bump.inputs['Strength'].default_value=.44*detail; bump.inputs['Distance'].default_value=.011
        l.new(crease.outputs[0],bump.inputs['Height']); l.new(nm.outputs['Normal'],bump.inputs['Normal']); l.new(bump.outputs['Normal'],p.inputs['Normal'])
        p.inputs['Subsurface Weight'].default_value=.028
    return m
paper=material('Ivory handmade paper',(.87,.765,.61),detail=.85)
facepaper=material('Soft ivory face',(.98,.90,.76),detail=.12)
inside=material('Warm paper inner layers',(.85,.735,.565),detail=.48)
red=material('Terracotta bookmark',(.40,.094,.037),detail=.35)
eye=material('Amber ink glass eyes',(.038,.012,.003),False,.16)

def mesh(name,verts,faces,mat,uvs=None,thickness=0):
    d=bpy.data.meshes.new(name); d.from_pydata(verts,[],faces); d.update()
    o=bpy.data.objects.new(name,d); scene.collection.objects.link(o); d.materials.append(mat)
    for f in d.polygons:f.use_smooth=True
    uv=d.uv_layers.new(name='PaperUV')
    for poly in d.polygons:
        for li in poly.loop_indices:
            vi=d.loops[li].vertex_index
            uv.data[li].uv=uvs[vi] if uvs else (verts[vi][0],verts[vi][2])
    parts.append(o)
    # Weld the wrap seam and cap collapsed ring tips, keeping loop UVs intact.
    bm=bmesh.new();bm.from_mesh(d)
    bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.00001)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(d);bm.free()
    if thickness:
        d.materials.append(mat if mat in (red,facepaper) else inside)
        s=o.modifiers.new('Real paper thickness','SOLIDIFY'); s.thickness=thickness; s.offset=0; s.material_offset=1; s.material_offset_rim=1
    return o

def grid(name,fn,nu,nv,mat=paper,thickness=.004,uvscale=(1,1)):
    v=[]; uv=[]; f=[]
    for j in range(nv+1):
        t=j/nv
        for i in range(nu+1):
            u=i/nu; p=Vector(fn(u,t))
            v.append(tuple(p)); uv.append((u*uvscale[0],t*uvscale[1]))
            if i<nu and j<nv:
                q=j*(nu+1)+i; f.append((q,q+1,q+nu+2,q+nu+1))
    return mesh(name,v,f,mat,uv,thickness)

def ball(name,loc,scale,mat,segments=48,rings=32):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,location=loc)
    o=bpy.context.object; o.name=name; o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(mat); parts.append(o)
    for p in o.data.polygons:p.use_smooth=True
    return o

def crinkle(p,amp=.002):
    return noise.noise_vector(Vector(p)*27,noise_basis='PERLIN_ORIGINAL')*amp

# Rounded recessed face, with shallow sculpted cheeks instead of separate cheek balls.
head=ball('Spirit_Face',(0,-.085,1.315),(.351,.313,.346),facepaper,64,40)
for v in head.data.vertices:
    x,y,z=v.co
    v.co.y-=.010*math.exp(-((abs(x)-.19)/.11)**2-((z+.09)/.09)**2)*(max(0,-y)/.313)
for side in [-1,1]:
    x=side*.143; z=1.325
    yy=-.085-.313*math.sqrt(1-(x/.351)**2-((z-1.315)/.346)**2)
    e=ball('Spirit_Eye_'+('L' if side<0 else 'R'),(x,yy-.007,z),(.042,.019,.071),eye)
    e.rotation_euler.z=side*math.radians(14)

# Hood: one continuous open, domed sheet with a softly irregular opening.
def hood(u,t):
    # A single tapered shell: the brow, crown and curled point share their rings.
    # No overlapping flap at the crown. A soft longitudinal fold replaces the old cap seam.
    a=math.tau*u; r=max(0,math.cos(t*math.pi/2))**.70
    s=math.sin(a); c=math.cos(a)
    fold=(.025*math.cos(3*a+.35)+.013*math.sin(7*a+3*t-.2))*math.sin(math.pi*t)
    x=(.487*r+fold)*c+.025*t*t
    z=1.345+.008*math.sin(math.pi*t)+(.466 if s>0 else .390)*r*s
    z+=(.075*max(0,s)+fold*s)*math.sin(math.pi*t)
    # Peaked brow and two relaxed corners, with a very shallow turned paper lip.
    brow=max(0,s)**12
    z+=.043*brow*(1-t)**2+.012*math.cos(3*a+.5)*(1-t)**3
    y=-.302+1.04*t-.13*t**6-.095*max(0,s)*r*r
    y-=.035*brow*(1-t)**3
    y+=.012*math.sin(5*a+.4)*(1-t)**4
    z+=.16*t**8
    p=Vector((x,y,z)); p+=noise.noise_vector(p*14)*.008*math.sin(math.pi*t)
    return p
hoodobj=grid('Spirit_Hood_single_paper_shell',hood,112,48,thickness=.0045,uvscale=(2.6,1.3))

# A broad curled continuation of the back crown. Root hidden within crown, fine tip rolls inward.
def curl_sheet(name,centers,widths,across=(1,0,0),mat=paper):
    # Catmull-Rom interpolation for smooth width/centre changes, not a stack of primitives.
    def at(t):
        q=t*(len(centers)-1); k=min(len(centers)-2,int(q)); a=q-k
        def interp(vals):
            p0=vals[max(0,k-1)];p1=vals[k];p2=vals[k+1];p3=vals[min(len(vals)-1,k+2)]
            return .5*((2*p1)+(-p0+p2)*a+(2*p0-5*p1+4*p2-p3)*a*a+(-p0+3*p1-3*p2+p3)*a*a*a)
        return interp([Vector(c) for c in centers]),max(.0004,interp(widths))
    def fn(u,t):
        p,w=at(t); s=u*2-1
        p+=Vector(across)*w*s
        p.y+=.030*(1-s*s)*math.sin(math.pi*t)
        if 'Hood' in name:p.z-=.083*s*s*(1-t)**3
        p+=crinkle(p,.0017)*abs(s)**4
        return p
    return grid(name,fn,28,72,mat,.0035,(1.3,1.8))
# The pointed hood is already part of Spirit_Hood_single_paper_shell.

# Core narrows continuously into a small curling ghost tail. No feet or separate stacked body forms.
def core(u,t):
    a=u*math.tau
    z=.97-.74*t
    r=.12+.145*math.sin(math.pi*t**.65)
    r*=1-.90*t**3
    x=.015*t+.08*t**4
    y=.018+.02*t
    return (x+r*math.cos(a),y+r*.79*math.sin(a),z)
grid('Spirit_rounded_body',core,64,42,facepaper,.003,(1.5,1.7))
curl_sheet('Spirit_single_curled_tail',[(.02,.03,.40),(.03,.045,.29),(.06,.08,.20),(.09,.12,.12),(.126,.168,.10),(.162,.216,.14),(.16,.215,.20),(.141,.188,.215)],[.14,.115,.083,.059,.038,.022,.010,.001],across=(.8,-.6,0))

# Five broad asymmetric paper panels, layered like overlapping pages, not pleated cloth.
def cloak(name,a0,a1,length,radius,phase,zoff=0):
    def fn(u,t):
        a=a0+(a1-a0)*u+.13*math.sin(math.pi*t)*math.sin(phase)
        r=.122+(radius-.122)*math.sin(t*math.pi*.53)
        r+=.026*math.sin(u*math.pi*2+phase)*t*t
        # Free hem curls outward and lifts; avoid straight conical hems.
        end=max(0,(t-.76)/.24)
        r+=.058*math.sin(end*math.pi*.75)*(0.3+0.7*math.sin(math.pi*u)**2)
        zz=1.005+zoff-length*t+.09*end**2
        zz+=.054*math.cos(math.pi*2*u+phase)*t**3
        r+=.018*math.cos(u*math.pi*3+phase)*math.sin(math.pi*t)
        p=Vector((r*math.sin(a),-r*.79*math.cos(a),zz))
        p+=crinkle(p,.0025)*math.sin(math.pi*t)
        return p
    return grid(name,fn,44,48,paper,.0035,(1.4,1.7))
cloak('Spirit_Cape_back',.85,5.43,.55,.445,.4,-.018)
cloak('Spirit_Cape_lower_left',-2.12,.12,.695,.332,1.0,-.01)
cloak('Spirit_Cape_lower_right',-.04,2.2,.65,.363,2.3,-.017)
cloak('Spirit_Cape_front_left',-1.75,.04,.45,.414,.4,.006)
cloak('Spirit_Cape_front_right',-.01,1.77,.51,.402,2.5,.009)

# Open bell sleeves from shoulder to hand, each shaped as a continuous paper tube.
for side in [-1,1]:
    def sleeve(u,t,side=side):
        a=u*math.tau; r=.064+.067*t
        x=side*(.145+.297*t)
        y=-.008-.085*t+r*math.cos(a)
        z=.947-.159*t+r*math.sin(a)
        x+=side*.015*math.sin(3*a)*t**4
        return (x,y,z)
    grid('Spirit_Sleeve_'+str(side),sleeve,48,26,paper,.0038,(1,1))
    hand=ball('Spirit_Mitten_'+str(side),(side*.435,-.096,.739),(.076,.070,.092),facepaper)
    hand.rotation_euler.y=-side*.49

# Front fork-ended bookmark with a slight fold, fully modeled thickness.
def bookmark(u,t):
    s=u*2-1
    z=.978-.186*t+.042*(1-abs(s))*t**9
    return (.060*s*(1+.10*t),-.321-.012*t+.012*math.sin(math.pi*t)+.009*s*s,z)
tag=grid('Spirit_red_bookmark',bookmark,18,24,red,.004,(.5,.8))

root=bpy.data.objects.new('PaperHoodSpirit',None);scene.collection.objects.link(root)
for o in parts:o.parent=root

# Fine paper silhouette edge fibres, sparse and short, so the model remains readable.
random.seed(12)
for i in range(28):
    u=random.random(); p=Vector(hood(u,0)); tangent=Vector((p.x,0,p.z-1.345)).normalized()
    d=bpy.data.curves.new('Paper edge fibre','CURVE');d.dimensions='3D';d.bevel_depth=.00065;d.bevel_resolution=1
    sp=d.splines.new('POLY');sp.points.add(2)
    for k in range(3):
        q=p+tangent*(k*.002)+Vector((0,-k*.001,0));sp.points[k].co=(*q,1);sp.points[k].radius=1-k*.44
    o=bpy.data.objects.new('Spirit_edge_fibre',d);scene.collection.objects.link(o);o.data.materials.append(paper);o.parent=root;parts.append(o)

groundmat=material('Studio matte',(.37,.325,.27),False,.9)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.045));ground=bpy.context.object;ground.name='Studio';ground.data.materials.append(groundmat)
def area(name,loc,power,color,size):
    d=bpy.data.lights.new(name,'AREA');d.energy=power;d.color=color;d.shape='DISK';d.size=size
    o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler()
area('Soft window',(-3,-4,4.5),550,(1,.90,.76),3)
area('Cool ivory fill',(3,-2,2.5),210,(.85,.91,1),2.5)
area('Paper rim',(1,2.5,3.5),650,(1,.83,.59),2)
d=bpy.data.cameras.new('Model review');cam=bpy.data.objects.new('Model review',d);scene.collection.objects.link(cam);scene.camera=cam
d.type='ORTHO';d.ortho_scale=2.22
def view(pos,name,target=(0,0,.94)):
    cam.location=pos;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)

cam.location=(2.7,-4.8,2.2);cam.rotation_euler=(Vector((0,0,.94))-cam.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'PaperHoodSpirit.blend'))
view((2.7,-4.8,2.2),'01-three-quarter')
ground.hide_render=True
view((0,-5,.96),'02-front')
view((-5,0,.96),'03-left')
view((5,0,.96),'04-right')
view((0,5,1.1),'05-back')
ground.hide_render=True
view((0,0,5),'06-top')
view((0,0,-4),'07-bottom')
ground.hide_render=False
cam.location=(2.7,-4.8,2.2);cam.rotation_euler=(Vector((0,0,.94))-cam.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'PaperHoodSpirit.blend'))

# Export only actual character geometry, with modifiers evaluated, never studio objects.
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
root.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(SRC/'PaperHoodSpirit.fbx'),use_selection=True,object_types={'MESH','EMPTY','OTHER'},bake_anim=False,axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=True)

dg=bpy.context.evaluated_depsgraph_get();tris=0
for o in parts:
    ev=o.evaluated_get(dg);m=ev.to_mesh();m.calc_loop_triangles();tris+=len(m.loop_triangles)
    assert all(all(math.isfinite(c) for c in v.co) for v in m.vertices),o.name
    ev.to_mesh_clear()
report={'source':'ArtSource/PaperHoodSpiritV1/PaperHoodSpirit.blend','renderer':'Blender Cycles; actual mesh renders','geometry_objects':len(parts),'evaluated_triangles':tris,'texture_packed':True,'unity_scene_modified':False,'rigged':False,'material_note':'Native Blender contains full paper shading. FBX procedural wrinkle shading requires baking/rebuilding in Unity.'}
(OUT/'model-check.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('HOOD_SPIRIT_COMPLETE',json.dumps(report),flush=True)
