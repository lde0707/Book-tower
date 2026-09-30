"""Detailed continuous paper hat; preserve V1 source, bake and skin with existing pipeline.
Blender --background --python Tools/refine_spirit_hat.py -- model|bake|rig
"""
import bpy, math, sys, json
from pathlib import Path
from mathutils import Vector, noise
ROOT=Path(__file__).resolve().parents[1]
SRC=ROOT/'ArtSource/PaperHoodSpiritV2'
OUT=ROOT/'Docs/ArtReview/SpiritHatV2'
SRC.mkdir(parents=True,exist_ok=True);OUT.mkdir(parents=True,exist_ok=True)
stage=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else 'model'
if stage in ('bake','rig'):
    code=(ROOT/('Tools/'+stage+'_hood_spirit.py')).read_text(encoding='utf-8')
    code=code.replace('ArtSource/PaperHoodSpiritV1','ArtSource/PaperHoodSpiritV2').replace('Docs/ArtReview/SpiritRedesignV1/ModelV1','Docs/ArtReview/SpiritHatV2').replace('Docs/ArtReview/SpiritRedesignV1/AnimationV1','Docs/ArtReview/SpiritHatV2')
    if stage=='rig':
        code=code.replace("ROOT/'Assets/_BookTower/Art/PaperHoodSpirit'","ROOT/'Logs/F0ArtValidation/Assets/_BookTower/Art/PaperHoodSpirit'")
        code=code.replace("w=.75*smooth((p.y-.22)/.40)","w=.85*smooth((p.z-1.78)/.52)")
        code=code.replace("('HoodTip',(0,.27,1.68),'Head')","('HoodTip',(0,.10,1.82),'Head')")
    exec(compile(code,str(ROOT/('Tools/'+stage+'_hood_spirit.py')),'exec'))
else:
    # Reuse the established surface/UV builder, then reopen the untouched source.
    code=(ROOT/'Tools/model_hood_spirit.py').read_text(encoding='utf-8').split('# Rounded recessed face')[0]
    exec(compile(code,str(ROOT/'Tools/model_hood_spirit.py'),'exec'))
    SRC=ROOT/'ArtSource/PaperHoodSpiritV2';OUT=ROOT/'Docs/ArtReview/SpiritHatV2'
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/PaperHoodSpiritV1/PaperHoodSpirit.blend'))
    scene=bpy.context.scene;root=bpy.data.objects['PaperHoodSpirit']
    for o in list(root.children):
        if 'Hood' in o.name or 'edge_fibre' in o.name:bpy.data.objects.remove(o,do_unlink=True)
    paper=bpy.data.materials['Ivory handmade paper'];inside=bpy.data.materials['Warm paper inner layers']
    facepaper=bpy.data.materials['Soft ivory face'];red=bpy.data.materials['Terracotta bookmark']
    parts=[]
    def profile(t):
        # Brim turns continuously into crown; the last rings form a relaxed hooked tip.
        keys=[(0,.61,1.49,0,.00),(.17,.405,1.73,0,.02),(.40,.285,1.95,-.03,.06),(.65,.17,2.18,-.01,.08),(.82,.092,2.34,.085,.075),(.93,.044,2.355,.195,.05),(1,0,2.255,.25,.015)]
        k=next((i for i in range(len(keys)-1) if t<=keys[i+1][0]),len(keys)-2)
        a,b=keys[k],keys[k+1];q=(t-a[0])/(b[0]-a[0])
        # Smooth Hermite slopes avoid horizontal seams at the profile knots.
        vals=[]
        for j in range(1,5):
            lo=keys[max(0,k-1)];hi=keys[min(len(keys)-1,k+2)]
            m0=(b[j]-lo[j])/(b[0]-lo[0]);m1=(hi[j]-a[j])/(hi[0]-a[0])
            vals.append((2*q**3-3*q*q+1)*a[j]+(q**3-2*q*q+q)*m0*(b[0]-a[0])+(-2*q**3+3*q*q)*b[j]+(q**3-q*q)*m1*(b[0]-a[0]))
        return vals
    def hat(u,t,lift=0):
        a=u*math.tau;r,z,cx,cy=profile(t)
        wave=(.013*math.sin(7*a+7*t)+.007*math.sin(13*a-11*t))*math.sin(math.pi*t)**.5
        r=max(0,r+wave)
        # Front stays above the eyes; sides drape lower and have asymmetric rolled edges.
        z-=.105
        z+=(.12*(-math.sin(a))-.19*math.cos(a)**2+.025*math.cos(3*a+.6))*(1-t)**5
        edge=(1-t)**16
        r+=edge*(.055*math.sin(5*a+.5)+.017*math.sin(13*a))
        z+=edge*(.065*math.sin(5*a+.8)+.021*math.sin(11*a)+.008*math.sin(29*a))
        p=Vector((cx+r*math.cos(a),cy+r*.86*math.sin(a),z))
        n=Vector((math.cos(a),.86*math.sin(a),.6)).normalized()
        # Local corrugations have short ridges rather than a smooth fabric-like wave.
        ridge=(abs(math.sin(a*5.5+t*24))**.35-.65)*.008
        p+=n*(lift+(noise.noise(p*31)*.006+ridge)*math.sin(math.pi*t))
        return p
    grid('Spirit_Hood_continuous_folded_hat',hat,128,88,paper,.0045,(3,2.8))
    # Thin overlap courses follow the same curved shell, with lifted irregular free hems.
    for idx,(start,end) in enumerate([(.055,.13),(.235,.32),(.425,.50),(.61,.68)]):
        def layer(u,v,start=start,end=end,idx=idx):
            a=u*math.tau;t=start+(end-start)*v+.023*math.sin(2*a+idx)+.009*math.sin(5*a-idx)
            return hat(u,t,.005+.012*(1-v)**3+.003*math.sin(17*a+idx)**2*(1-v)**6)
        grid('Spirit_Hood_page_overlap_'+str(idx),layer,128,8,paper,.002,(3,.35))
    # Local folded facets, broad at their bases and tapering into the crown.
    for idx in range(9):
        center=idx/9+.017*math.sin(idx*2);start=.09+.045*(idx%3)
        def fold(u,v,center=center,start=start,idx=idx):
            width=.047*(1-.97*v);ang=center+(u-.5)*width*2
            t=start+.31*v
            rise=.025*(1-abs(u*2-1))*math.sin(math.pi*v)**.7
            return hat(ang,t,.004+rise)
        grid('Spirit_Hood_fold_facet_'+str(idx),fold,12,22,paper,.0018,(.4,1))
    for idx in range(64):
        u=(idx*.61803398875)%1
        def fiber(u0,v,u=u,idx=idx):
            p=hat(u,0,.001)
            direction=Vector((math.cos(u*math.tau),math.sin(u*math.tau),-.35))
            return p+direction*(v*(.003+.004*(idx%4)/3))+Vector((0,0,(u0-.5)*.0012*(1-v)))
        grid('Spirit_Hood_edge_fibre_'+str(idx),fiber,1,2,paper,0,(.1,.1))
    for o in parts:o.parent=root
    scene.render.resolution_x=1200;scene.render.resolution_y=1200;scene.cycles.samples=40
    cam=scene.camera;cam.data.ortho_scale=2.9
    def render(pos,name):
        cam.location=pos;cam.rotation_euler=(Vector((0,0,1.2))-cam.location).to_track_quat('-Z','Y').to_euler()
        scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
    cam.location=(2.7,-4.8,2.3);cam.rotation_euler=(Vector((0,0,1.2))-cam.location).to_track_quat('-Z','Y').to_euler()
    bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'PaperHoodSpirit.blend'))
    render((2.7,-4.8,2.3),'01-three-quarter')
    render((0,-5,1.8),'02-front')
    render((3,4,2.4),'03-rear-game-view')
    print('HAT_MODEL_COMPLETE',flush=True)
