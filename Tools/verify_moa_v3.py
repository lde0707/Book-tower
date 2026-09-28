"""Validate saved modelling source and leave its review camera ready to render."""
import bpy, json, math
from mathutils import Vector
from pathlib import Path
root=Path(__file__).resolve().parents[1]
source=root/'ArtSource/PaperMoaV3/Moa_SculptV3.blend'
bpy.ops.wm.open_mainfile(filepath=str(source))
scene=bpy.context.scene;scene.frame_set(1)
cam=scene.camera;cam.location=(-2.4,-3.5,1.65)
cam.rotation_euler=(Vector((0,.04,.47))-cam.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(root/'Docs/ArtReview/MoaModelViewsV1/SculptV3/three-quarter.png')
dg=bpy.context.evaluated_depsgraph_get();triangles=0;objects=0
for obj in scene.objects:
    if obj.type not in {'MESH','CURVE'} or not obj.name.startswith('Moa'):continue
    evaluated=obj.evaluated_get(dg);mesh=evaluated.to_mesh()
    assert all(math.isfinite(v.co.x) and math.isfinite(v.co.y) and math.isfinite(v.co.z) for v in mesh.vertices),obj.name
    mesh.calc_loop_triangles();triangles+=len(mesh.loop_triangles);objects+=1;evaluated.to_mesh_clear()
assert source.exists() and (source.parent/'Moa_SculptV3.fbx').stat().st_size>100000
assert bpy.data.images.get('PaperFiber.png') and bpy.data.images['PaperFiber.png'].packed_file
bpy.ops.wm.save_as_mainfile(filepath=str(source))
report={'valid_geometry_objects':objects,'sculpt_render_triangles':triangles,'paper_texture_packed':True,'source_reopened':True,'preview_camera_saved':True,'game_runtime_optimized':False,'unity_scene_changed':False}
(root/'Docs/ArtReview/MoaModelViewsV1/SculptV3/source-check.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('MOA_SOURCE_VERIFIED',json.dumps(report))
