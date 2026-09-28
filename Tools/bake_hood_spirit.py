"""Bake the approved sculpt material into a portable PBR atlas, export and round-trip verify."""
import bpy, json, math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
SRC=ROOT/'ArtSource/PaperHoodSpiritV1'
OUT=ROOT/'Docs/ArtReview/SpiritRedesignV1/ModelV1'
TEX=SRC/'Textures'; TEX.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(SRC/'PaperHoodSpirit.blend'))
scene=bpy.context.scene;scene.cycles.samples=8
root=bpy.data.objects.get('PaperHoodSpirit')
objs=[o for o in scene.objects if o.parent==root]
bpy.ops.object.select_all(action='DESELECT')
for o in objs:
    o.select_set(True)
    if o.type=='MESH' and o.data.uv_layers:
        o.data.uv_layers.active.name='PaperUV'
bpy.context.view_layer.objects.active=objs[0]
bpy.ops.object.convert(target='MESH')
bpy.ops.object.join(); obj=bpy.context.object;obj.name='PaperHoodSpirit_Baked'
bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
# Source texture UVs stay explicit while a second nonoverlapping UV set receives the bake.
for m in obj.data.materials:
    for n in m.node_tree.nodes:
        if n.type=='TEX_IMAGE' and not n.inputs['Vector'].is_linked:
            uv=m.node_tree.nodes.new('ShaderNodeUVMap');uv.uv_map='PaperUV'
            m.node_tree.links.new(uv.outputs['UV'],n.inputs['Vector'])
obj.data.uv_layers.new(name='Atlas')
obj.data.uv_layers.active_index=len(obj.data.uv_layers)-1
obj.data.uv_layers.active.active_render=True
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(angle_limit=math.radians(70),island_margin=.008,area_weight=.2)
bpy.ops.object.mode_set(mode='OBJECT')
scene.render.bake.use_selected_to_active=False;scene.render.bake.margin=12
scene.render.bake.use_pass_direct=False;scene.render.bake.use_pass_indirect=False;scene.render.bake.use_pass_color=True
images={}
for name,typ,cs in [('BaseColor','DIFFUSE','sRGB'),('Normal','NORMAL','Non-Color'),('Roughness','ROUGHNESS','Non-Color')]:
    image=bpy.data.images.new('Spirit_'+name,width=2048,height=2048,alpha=False)
    image.colorspace_settings.name=cs
    for m in obj.data.materials:
        n=m.node_tree.nodes.new('ShaderNodeTexImage');n.image=image;m.node_tree.nodes.active=n
    bpy.ops.object.bake(type=typ)
    image.filepath_raw=str(TEX/('Spirit_'+name+'.png'));image.file_format='PNG';image.save()
    images[name]=image
    print('BAKED',name,flush=True)

atlas=obj.data.uv_layers.get('Atlas')
for uv in list(obj.data.uv_layers):
    if uv.name!='Atlas':obj.data.uv_layers.remove(uv)
obj.data.uv_layers.active_index=0
mat=bpy.data.materials.new('Spirit_PBR_Atlas');mat.use_nodes=True
n=mat.node_tree.nodes;l=mat.node_tree.links;p=n.get('Principled BSDF')
for name,slot in [('BaseColor','Base Color'),('Roughness','Roughness')]:
    tx=n.new('ShaderNodeTexImage');tx.image=images[name];l.new(tx.outputs['Color'],p.inputs[slot])
tx=n.new('ShaderNodeTexImage');tx.image=images['Normal'];nm=n.new('ShaderNodeNormalMap')
l.new(tx.outputs['Color'],nm.inputs['Color']);l.new(nm.outputs[0],p.inputs['Normal'])
obj.data.materials.clear();obj.data.materials.append(mat)
for f in obj.data.polygons:f.material_index=0
obj.data.calc_loop_triangles()
assert len(obj.data.loop_triangles)>10000
assert all(all(math.isfinite(c) for c in v.co) for v in obj.data.vertices)
origdim=list(obj.dimensions)
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'PaperHoodSpirit_Baked.blend'))
bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);root.select_set(True)
bpy.ops.export_scene.gltf(filepath=str(SRC/'PaperHoodSpirit.glb'),export_format='GLB',use_selection=True,export_animations=False,export_yup=True)
bpy.ops.export_scene.fbx(filepath=str(SRC/'PaperHoodSpirit_Baked.fbx'),use_selection=True,object_types={'MESH','EMPTY'},bake_anim=False,axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=True)

# Reimport GLB into the review stage, so the preview validates the portable asset as well.
obj.hide_render=True
bpy.ops.object.select_all(action='DESELECT')
bpy.ops.import_scene.gltf(filepath=str(SRC/'PaperHoodSpirit.glb'))
imported=[o for o in bpy.context.selected_objects if o.type=='MESH']
assert imported,'GLB contains no meshes'
size=list(imported[0].dimensions)
assert max(abs(a-b) for a,b in zip(size,origdim))<.002,(size,origdim)
scene.cycles.samples=48;scene.render.filepath=str(OUT/'08-export-verified.png')
bpy.ops.render.render(write_still=True)
report={'source_reopened':True,'portable_glb_reimported':True,'dimensions_m':size,'triangles':len(obj.data.loop_triangles),'pbr_textures':[p.name for p in TEX.glob('*.png')],'texture_resolution':2048,'rigged':False,'unity_applied':False}
(OUT/'export-check.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('HOOD_EXPORT_VERIFIED',json.dumps(report),flush=True)
