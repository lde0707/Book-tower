"""V7: soften paper creases without changing the approved silhouette.
Blender --background --python Tools/refine_spirit_soft_paper.py -- model|bake|rig
Always starts from V6; never accumulates smoothing on a prior output.
"""
import sys, json, math
from pathlib import Path
import bpy
import numpy as np
from mathutils import Vector, kdtree

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / 'ArtSource/SpiritSoftPaperV7'
OUT = ROOT / 'Docs/ArtReview/SpiritSoftPaperV7'
SRC.mkdir(parents=True, exist_ok=True)
OUT.mkdir(parents=True, exist_ok=True)
stage = sys.argv[sys.argv.index('--') + 1] if '--' in sys.argv else 'model'
if stage == 'bake':
    code = (ROOT/'Tools/bake_hood_spirit.py').read_text(encoding='utf-8')
    code = code.replace('ArtSource/PaperHoodSpiritV1', 'ArtSource/SpiritSoftPaperV7')
    code = code.replace('Docs/ArtReview/SpiritRedesignV1/ModelV1', 'Docs/ArtReview/SpiritSoftPaperV7')
    exec(compile(code, str(ROOT/'Tools/bake_hood_spirit.py'), 'exec'))
    raise SystemExit(0)
if stage == 'rig':
    code = (ROOT/'Tools/rig_turnaround_spirit.py').read_text(encoding='utf-8')
    code = code.replace('TurnaroundSpiritModelV6', 'SpiritSoftPaperV7')
    # Keep all ten states, bones and durations. Only the walking pose is revised.
    code = code.replace('stride=24 if running else 13', 'stride=24 if running else 32')
    code = code.replace('(3 if running else 1.5)*sw', '(3 if running else 4.5)*sw')
    code = code.replace("rot('Head',x=-5 if running else -2)", "rot('Head',x=-5 if running else -3,y=0 if running else -2.5*sw)")
    code = code.replace("move('MotionRoot',z=(.018 if running else .008)*(1-math.cos(phase*2)))", "move('MotionRoot',z=.018*(1-math.cos(phase*2)) if running else .012*math.cos(phase*2))")
    code = code.replace('-(24 if running else 12)*lift', '-(24 if running else -26)*lift')
    code = code.replace("rot('Leg'+side,x=stride*swing)", "rot('Leg'+side,x=stride*swing*(1 if running else -1))\n                if not running:move('Leg'+side,y=-.10*swing,z=.09*lift-.05*abs(swing))")
    code = code.replace("rot('Foot'+side,x=-stride*swing*.35)", "rot('Foot'+side,x=-stride*swing*.35*(1 if running else -1))")
    code = code.replace('-(22 if running else 10)*swing', '-(22 if running else 25)*swing')
    code = code.replace('(4 if running else 2)*late', '(4 if running else 6)*late')
    code = code.replace('(7 if running else 3)*math.sin', '(7 if running else 9)*math.sin')
    code = code.replace("        elif name=='JumpRise':", "            if not running:\n                for hem in ('CapeLTip','CapeRTip','CapeBackTip'):move(hem,z=.08*abs(sw))\n        elif name=='JumpRise':")
    exec(compile(code, str(ROOT/'Tools/rig_turnaround_spirit.py'), 'exec'))
    raise SystemExit(0)

bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/TurnaroundSpiritModelV6/PaperHoodSpirit.blend'))
scene = bpy.context.scene
prefs = bpy.context.preferences.addons['cycles'].preferences
prefs.compute_device_type = 'OPTIX'
prefs.get_devices()
for device in prefs.devices: device.use = device.type != 'CPU'
scene.cycles.device = 'GPU'
root = bpy.data.objects['PaperHoodSpirit']
report = {'base': 'TurnaroundSpiritModelV6', 'mesh': [], 'materials': []}

def soften(obj, iterations, limit):
    """Constrained fairing; boundary and its first ring remain exactly fixed.
    Remove dense surface corrugation, not the frayed silhouette or large folds.
    """
    mesh = obj.data
    count = len(mesh.vertices)
    original = np.array([v.co[:] for v in mesh.vertices], dtype=np.float64)
    edges = np.array([e.vertices[:] for e in mesh.edges], dtype=int)
    uses = {}
    for poly in mesh.polygons:
        ids = list(poly.vertices)
        for a, b in zip(ids, ids[1:] + ids[:1]):
            key = tuple(sorted((a, b)))
            uses[key] = uses.get(key, 0) + 1
    pinned = {i for e, n in uses.items() if n == 1 for i in e}
    rim = set(pinned)
    for a, b in edges:
        if a in rim or b in rim: pinned.update((a, b))
    mobile = np.ones(count, dtype=bool)
    mobile[list(pinned)] = False
    degree = np.bincount(edges.ravel(), minlength=count).clip(1)
    current = original.copy()
    for _ in range(iterations):
        sums = np.zeros_like(current)
        np.add.at(sums, edges[:, 0], current[edges[:, 1]])
        np.add.at(sums, edges[:, 1], current[edges[:, 0]])
        lap = sums / degree[:, None] - current
        current[mobile] += .38 * lap[mobile]
        delta = current - original
        distance = np.linalg.norm(delta, axis=1)
        current = original + delta * np.minimum(1, limit / np.maximum(distance, 1e-12))[:, None]
    for vertex, xyz in zip(mesh.vertices, current): vertex.co = xyz
    mesh.update()
    report['mesh'].append({'part': obj.name, 'vertices': count, 'fixed_edge_vertices': len(pinned),
                           'max_change_m': float(np.linalg.norm(current-original, axis=1).max())})
    return original, current

hat = bpy.data.objects['Spirit_Hood_continuous_folded_hat']
old, new = soften(hat, 24, .007)
tree = kdtree.KDTree(len(old))
for i, p in enumerate(old): tree.insert(Vector(p), i)
tree.balance()
# The overlays follow the exact displacement of the underlying paper, avoiding
# new intersections between a smoothed crown and unchanged narrow fold strips.
for obj in root.children:
    if obj.type != 'MESH' or obj == hat: continue
    if obj.name.startswith(('Spirit_Hood_page_overlap_', 'Spirit_Hood_fold_facet_')):
        for vertex in obj.data.vertices:
            nearest = tree.find_n(vertex.co, 4)
            weights = np.array([1/max(distance, .0001)**2 for _, _, distance in nearest])
            delta = sum((new[index]-old[index])*weight for (_, index, _), weight in zip(nearest, weights))/weights.sum()
            vertex.co += Vector(delta)
        soften(obj, 8, .0025)
    elif obj.name.startswith(('Spirit_Cape_', 'Spirit_Scarf_', 'Spirit_Sleeve_')) and len(obj.data.vertices)>100:
        soften(obj, 12, .0025)

# Fibers stay visible, but coarse cracks and pressed pulp no longer look like
# deep stone/leather relief. Glass eyes and the existing ivory palette stay intact.
for mat in bpy.data.materials:
    if not mat.use_nodes or 'paper' not in mat.name.lower() and mat.name != 'Soft ivory face': continue
    changes = []
    for node in mat.node_tree.nodes:
        if node.type == 'NORMAL_MAP':
            before = node.inputs['Strength'].default_value
            node.inputs['Strength'].default_value = min(before, .24 if 'face' not in mat.name else .10)
            changes.append(('fiber_normal', before, node.inputs['Strength'].default_value))
        elif node.type == 'BUMP':
            before = node.inputs['Strength'].default_value
            # Fine pulp survives more strongly than the Voronoi cracked-paper layer.
            source = node.inputs['Height'].links[0].from_node if node.inputs['Height'].is_linked else None
            fine = source and source.type == 'TEX_NOISE' and source.inputs['Scale'].default_value > 100
            node.inputs['Strength'].default_value = before * (.38 if fine else .20)
            node.inputs['Distance'].default_value *= .5
            changes.append(('bump', before, node.inputs['Strength'].default_value))
        elif node.type == 'MIX_RGB' and node.blend_type == 'MULTIPLY':
            # V3 added dark pore tint; keep original fiber/base-color connections.
            fac = node.inputs[0]
            if not fac.is_linked and abs(fac.default_value-.52)<.001: fac.default_value=.20
    if changes: report['materials'].append({'name': mat.name, 'changes': changes})

scene.render.resolution_x = scene.render.resolution_y = 1200
scene.cycles.samples = 40
camera = scene.camera
camera.data.type = 'ORTHO'
camera.data.ortho_scale = 2.65
def shot(pos, name):
    camera.location = pos
    camera.rotation_euler = (Vector((0,0,1.13))-camera.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath = str(OUT/(name+'.png'))
    bpy.ops.render.render(write_still=True)
camera.location = (2.7,-4.8,2.1)
camera.rotation_euler = (Vector((0,0,1.13))-camera.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'PaperHoodSpirit.blend'))
(OUT/'mesh-material-check.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
shot((2.7,-4.8,2.1), 'sculpt-front')
shot((2.4,4.8,2.2), 'sculpt-rear')
print('SOFT_PAPER_MODEL_COMPLETE', flush=True)
