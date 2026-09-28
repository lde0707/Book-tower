import bpy, math, json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Docs/ArtReview/SpiritRedesignV1/AnimationV1'
FRAMES=ROOT/'Logs/HoodAnimationFrames';FRAMES.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/PaperHoodSpiritV1/PaperHoodSpirit_Rigged.blend'))
s=bpy.context.scene;arm=bpy.data.objects['SpiritRig']
s.render.engine='BLENDER_EEVEE';s.eevee.taa_render_samples=8
s.render.use_simplify=True;s.render.simplify_subdivision_render=0
for o in s.objects:
    if o.type=='LIGHT':o.data.use_shadow=o.name=='Soft window'
s.render.resolution_x=640;s.render.resolution_y=720;s.render.resolution_percentage=100
fps=24;count=0;timeline=[]
for name,seconds in [('Idle',2),('Move',1.8),('JumpRise',.6),('Fall',.7),('Land',.45),('LookAround',4.8),('TidyBookmark',3.8),('Stretch',4.5)]:
    arm.animation_data.action=bpy.data.actions[name];start=count
    action_duration=(arm.animation_data.action.frame_range[1]-1)/60
    for k in range(round(seconds*fps)):
        t=k/fps
        if name in ['Idle','Move','Fall']:t=t%action_duration
        s.frame_set(1+int(t*60),subframe=t*60%1)
        s.render.filepath=str(FRAMES/('%04d.png'%count));bpy.ops.render.render(write_still=True);count+=1
    timeline.append({'clip':name,'from_seconds':start/fps,'to_seconds':count/fps})
(OUT/'preview-timeline.json').write_text(json.dumps({'fps':fps,'frames':count,'timeline':timeline},indent=2))
print('ANIMATION_FRAMES_DONE',count,flush=True)
