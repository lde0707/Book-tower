"""Render genuine Blender animation frames, without image retouching."""
import bpy,json,sys,time
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Docs/ArtReview/MoaModelViewsV1/FlightV1'
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/PaperMoaV3/Moa_FlightV1.blend'))
scene=bpy.context.scene
scene.render.engine='BLENDER_EEVEE'
scene.render.resolution_x=720;scene.render.resolution_y=600;scene.render.resolution_percentage=100
try:scene.eevee.taa_render_samples=8
except AttributeError:pass
scene.render.use_simplify=True;scene.render.simplify_subdivision_render=0
for obj in scene.objects:
    if obj.type=='CURVE':obj.data.bevel_resolution=1;obj.data.resolution_u=5
    if obj.type=='LIGHT':obj.data.use_shadow=obj.name=='Large soft window'
print('RENDER_SETUP',scene.render.engine,[(p.identifier,str(getattr(scene.eevee,p.identifier))) for p in scene.eevee.bl_rna.properties if 'sample' in p.identifier],flush=True)
scene.render.image_settings.file_format='PNG'
frames=ROOT/'Logs/MoaFlightRenderFrames';frames.mkdir(parents=True,exist_ok=True)
targets=[bpy.data.objects[n] for n in ['Moa_Character','Preview_follow_path','Moa_Wing_L','Moa_Wing_R','Moa_Foot_-1','Moa_Foot_1','Player_scale_reference']]
scene.frame_set(1);start=[o.matrix_world.copy() for o in targets]
scene.frame_set(481);end=[o.matrix_world.copy() for o in targets]
seam=max(abs(a[i][j]-b[i][j]) for a,b in zip(start,end) for i in range(4) for j in range(4))
assert seam<.0001,('Animation loop discontinuity',seam)
began=time.time()
for i,f in enumerate([1,7,178] if '--test' in sys.argv else range(1,481,2)):
    scene.frame_set(f);scene.render.filepath=str(frames/(('%04d'%i)+'.png'));bpy.ops.render.render(write_still=True)
    if i%24==0:print('MOA_FRAME',i,'elapsed',round(time.time()-began,1),flush=True)
report={'loop_matrix_error':seam,'frames':i+1,'render_fps':30,'authored_fps':60,'seconds':8,'renderer':'Blender EEVEE actual 3D frame sequence','unity_scene_changed':False}
(OUT/'render-check.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('MOA_RENDER_COMPLETE',json.dumps(report),flush=True)
