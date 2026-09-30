"""V4: broad irregular paper folds instead of repeated rectangular scales.
Blender --background --python Tools/refine_spirit_garment.py -- model|bake|rig
"""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
code=(ROOT/'Tools/refine_spirit_layers.py').read_text(encoding='utf-8')
code=code.replace('PaperHoodSpiritV3','PaperHoodSpiritV4').replace('SpiritLayersV3','SpiritGarmentV4')
start=code.index('for name,*params in panels:')
end=code.index('\nfor o in parts:o.parent=root',start)
code=code[:start]+'''
# Open fibrous paper texture: no closed polygon/cracked-skin pattern on the garment.
fiber=bpy.data.images.load(str(ROOT/'Assets/_BookTower/Art/PaperSpirit/PaperFiber.png'),check_existing=True)
normal=bpy.data.images.load(str(ROOT/'Assets/_BookTower/Art/PaperSpirit/PaperFiberNormal.png'),check_existing=True)
normal.colorspace_settings.name='Non-Color'
garment=material('Soft fibrous garment paper',(.91,.815,.67),rough=.88,detail=.48)
for o in list(root.children):
    if 'Cape' in o.name or 'Sleeve' in o.name:
        o.data.materials.clear();o.data.materials.append(garment);o.data.materials.append(inside)

# Two long, broad diagonal folds per panel, with individually offset torn hems.
# No repeated rows or little rectangular patches.
for panel_index,(name,*params) in enumerate(panels):
    for leaf in range(2):
        def page(u,v,leaf=leaf,params=params,panel_index=panel_index):
            center=.29+leaf*.40+.055*math.sin(panel_index*2+leaf)
            span=.53*(1-.27*v)+.045*math.sin(math.pi*v)
            uu=center+(u-.5)*span+.055*math.sin(math.pi*v+leaf)
            start=.13+.045*math.sin(panel_index+leaf*2)
            # Slanted broad free edge and a restrained irregular torn lip.
            length=.59+.11*math.sin(panel_index*1.7+leaf)+.09*math.sin(u*math.pi+.8*leaf)
            torn=.003*math.sin(31*u+panel_index)+.002*math.sin(63*u+leaf)
            tt=start+v*(length+.10*(u-.5)+torn)
            p,n=cloak_surface(uu,tt,*params)
            lift=.005+.007*math.sin(math.pi*u)**2+.012*v**5
            lift+=.010*math.sin(math.pi*u)*math.sin(math.pi*v)
            p+=n*lift
            p.z+=.018*math.sin(u*math.pi*1.5+leaf)*v**6
            return p
        grid('Spirit_Cape_'+name+'_broad_fold_'+str(leaf),page,36,30,garment,.0015,(1.2,1.6))
''' + code[end:]
exec(compile(code,str(ROOT/'Tools/refine_spirit_layers.py'),'exec'))
