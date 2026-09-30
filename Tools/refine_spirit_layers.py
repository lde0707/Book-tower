"""V3: enveloping hood, layered paper scarf and tactile handmade-paper clothes.
Blender --background --python Tools/refine_spirit_layers.py -- model|bake|rig
"""
import bpy, math, sys, json
from pathlib import Path
from mathutils import Vector, noise
ROOT=Path(__file__).resolve().parents[1]
stage=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else 'model'
code=(ROOT/'Tools/refine_spirit_hat.py').read_text(encoding='utf-8')
code=code.replace('PaperHoodSpiritV2','PaperHoodSpiritV3').replace('SpiritHatV2','SpiritLayersV3')
if stage=='model':
    code=code.split('    for o in parts:o.parent=root')[0]
    code=code.replace('z-=.105','z-=.127+.095*math.cos(a)**2*(1-t)**5')
    code=code.replace('r=max(0,r+wave)','r=max(0,r+wave-.035*math.cos(a)**2*(1-t)**5)')
if stage=='rig':
    # Scarf rests on the body while the small loose ends use the cloak secondary motion.
    code=code.replace("if stage=='rig':", "if stage=='rig':\n        code=code.replace(\"    if 'Eye_' in name:\",\"    if 'Scarf_wrap' in name:return {'Body':.88,'Head':.12}\\n    if 'Scarf_tail' in name:return chain(p,'Body','CapeBackTip',1.03,.76)\\n    if 'Eye_' in name:\")")
exec(compile(code,str(ROOT/'Tools/refine_spirit_hat.py'),'exec'))
if stage!='model':raise SystemExit(0)

def handmade(mat,strength,scale):
    """Combine coarse pressed-paper wrinkles, fine pulp and sparse warm fibers for baking."""
    n=mat.node_tree.nodes;l=mat.node_tree.links;p=n.get('Principled BSDF')
    oldnormal=p.inputs['Normal'].links[0].from_socket if p.inputs['Normal'].is_linked else None
    oldcolor=p.inputs['Base Color'].links[0].from_socket if p.inputs['Base Color'].is_linked else None
    coord=n.new('ShaderNodeTexCoord')
    vor=n.new('ShaderNodeTexVoronoi');vor.feature='DISTANCE_TO_EDGE';vor.inputs['Scale'].default_value=scale
    warp=n.new('ShaderNodeTexNoise');warp.inputs['Scale'].default_value=11;warp.inputs['Detail'].default_value=2
    l.new(coord.outputs['Object'],warp.inputs['Vector'])
    warp_scale=n.new('ShaderNodeVectorMath');warp_scale.operation='SCALE';warp_scale.inputs[3].default_value=.035
    l.new(warp.outputs['Color'],warp_scale.inputs[0])
    warp_add=n.new('ShaderNodeVectorMath');warp_add.operation='ADD';l.new(coord.outputs['Object'],warp_add.inputs[0]);l.new(warp_scale.outputs[0],warp_add.inputs[1]);l.new(warp_add.outputs[0],vor.inputs['Vector'])
    ridge=n.new('ShaderNodeValToRGB');ridge.color_ramp.elements[0].position=.0;ridge.color_ramp.elements[0].color=(.16,.16,.16,1)
    ridge.color_ramp.elements[1].position=.045;ridge.color_ramp.elements[1].color=(.75,.75,.75,1)
    l.new(vor.outputs['Distance'],ridge.inputs[0])
    bump=n.new('ShaderNodeBump');bump.inputs['Strength'].default_value=strength*.6;bump.inputs['Distance'].default_value=.004
    l.new(ridge.outputs['Color'],bump.inputs['Height'])
    if oldnormal:l.new(oldnormal,bump.inputs['Normal'])
    fine=n.new('ShaderNodeTexNoise');fine.inputs['Scale'].default_value=210;fine.inputs['Detail'].default_value=3
    l.new(coord.outputs['Object'],fine.inputs['Vector'])
    pulp=n.new('ShaderNodeBump');pulp.inputs['Strength'].default_value=strength*.65;pulp.inputs['Distance'].default_value=.003
    l.new(fine.outputs['Fac'],pulp.inputs['Height']);l.new(bump.outputs['Normal'],pulp.inputs['Normal']);l.new(pulp.outputs['Normal'],p.inputs['Normal'])
    tint=n.new('ShaderNodeValToRGB');tint.color_ramp.elements[0].position=.30;tint.color_ramp.elements[0].color=(.56,.40,.24,1)
    tint.color_ramp.elements[1].position=.44;tint.color_ramp.elements[1].color=(1,.99,.95,1)
    l.new(fine.outputs['Fac'],tint.inputs[0])
    mix=n.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=.32 if mat==facepaper else .52
    if oldcolor:l.new(oldcolor,mix.inputs[1])
    else:mix.inputs[1].default_value=p.inputs['Base Color'].default_value
    l.new(tint.outputs['Color'],mix.inputs[2]);l.new(mix.outputs[0],p.inputs['Base Color'])
    p.inputs['Roughness'].default_value=.88;p.inputs['Specular IOR Level'].default_value=.19
handmade(paper,.52,27);handmade(inside,.40,32);handmade(facepaper,.26,38)

# The face keeps its original silhouette and inset eyes, but small irregularities catch light.
face=bpy.data.objects['Spirit_Face']
for vertex in face.data.vertices:
    p=vertex.co;normal=p.normalized()
    vertex.co+=normal*noise.noise(p*52)*.0018

# Three broad paper ribbons spiral around the neck. Finite thickness and lifted rims cast shadows.
for band in range(3):
    def scarf(u,v,band=band):
        a=math.tau*u
        radius=.282+.009*band+.012*math.sin(3*a+band)+.009*math.sin(v*math.pi)
        z=.965+band*.047+(v-.5)*.085+.038*math.cos(a-.5)
        z+=.006*math.sin(13*a+band)*(abs(v-.5)*2)**3
        p=Vector((radius*math.cos(a),-.060+radius*.91*math.sin(a),z))
        p+=Vector((math.cos(a),math.sin(a),.3))*noise.noise(p*36)*.005
        return p
    grid('Spirit_Scarf_wrap_'+str(band),scarf,112,12,paper,.004,(2,.6))
for side in [-1,1]:
    def tail(u,t,side=side):
        w=.063*(1-.35*t)*(1+.1*math.sin(t*13))
        return (side*(.22+.31*t)+(u-.5)*w*2,.14+.19*t+.035*math.sin(math.pi*t),1.045-.25*t+.04*math.sin(t*math.pi)+(u-.5)*.07+noise.noise(Vector((u*10,t*12,side)))*.004)
    grid('Spirit_Scarf_tail_'+str(side),tail,16,30,paper,.003,(.5,1.5))

# Re-evaluate the same source cloak surface, then layer irregular torn pieces just above it.
def cloak_surface(u,t,a0,a1,length,radius,phase,zoff):
    a=a0+(a1-a0)*u+.13*math.sin(math.pi*t)*math.sin(phase)
    r=.122+(radius-.122)*math.sin(t*math.pi*.53)+.026*math.sin(u*math.pi*2+phase)*t*t
    end=max(0,(t-.76)/.24)
    r+=.058*math.sin(end*math.pi*.75)*(.3+.7*math.sin(math.pi*u)**2)
    r+=.018*math.cos(u*math.pi*3+phase)*math.sin(math.pi*t)
    z=1.005+zoff-length*t+.09*end**2+.054*math.cos(math.pi*2*u+phase)*t**3
    p=Vector((r*math.sin(a),-r*.79*math.cos(a),z));p+=crinkle(p,.0025)*math.sin(math.pi*t)
    return p,Vector((math.sin(a),-math.cos(a),.18)).normalized()
panels=[('back',.85,5.43,.55,.445,.4,-.018),('lower_left',-2.12,.12,.695,.332,1,-.01),('lower_right',-.04,2.2,.65,.363,2.3,-.017),('front_left',-1.75,.04,.45,.414,.4,.006),('front_right',-.01,1.77,.51,.402,2.5,.009)]
for name,*params in panels:
    for row in range(3):
        for col in range(4):
            def page(u,v,row=row,col=col,params=params):
                # Widen then taper the free edge; keep every sheet anchored to the cape.
                span=.215*(.86+.14*math.sin(math.pi*v))
                uu=.075+col*.235+(u-.5)*span+.022*math.sin(row*3+col)
                torn=.014*math.sin(17*u+col)+.008*math.sin(39*u+row)
                tt=.22+row*.24+v*(.24+torn+.035*math.sin(col*2+row))
                p,n=cloak_surface(uu,tt,*params)
                lift=.007+.018*v**3+.008*math.sin(math.pi*u)*math.sin(math.pi*v)
                p+=n*lift;p.z+=.012*math.sin(u*math.pi*3+col)*v**5
                return p
            grid('Spirit_Cape_'+name+'_paper_layer_'+str(row)+'_'+str(col),page,14,12,paper,.0022,(.65,.6))

for o in parts:o.parent=root
scene.render.resolution_x=1200;scene.render.resolution_y=1200;scene.cycles.samples=56
cam=scene.camera;cam.data.ortho_scale=2.85
cam.location=(2.7,-4.8,2.15);cam.rotation_euler=(Vector((0,0,1.17))-cam.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'PaperHoodSpirit.blend'))
for pos,name,target,zoom in [((2.7,-4.8,2.15),'01-three-quarter',(0,0,1.17),2.85),((0,-5,1.6),'02-front',(0,0,1.17),2.85),((3,4,2.3),'03-rear',(0,0,1.17),2.85),((1.6,-4,1.85),'04-paper-detail',(0,-.07,1.23),1.55)]:
    cam.location=pos;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=zoom
    scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
print('PAPER_LAYERS_MODEL_COMPLETE',flush=True)
