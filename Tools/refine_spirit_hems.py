"""V5: lightly worn paper hems without repeating serrations or scale patterns."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
code=(ROOT/'Tools/refine_spirit_garment.py').read_text(encoding='utf-8')
code=code.replace('PaperHoodSpiritV4','PaperHoodSpiritV5').replace('SpiritGarmentV4','SpiritHemV5')
code=code.replace('torn=.003*math.sin(31*u+panel_index)+.002*math.sin(63*u+leaf)', '''torn=.004*math.sin(31*u+panel_index)+.003*math.sin(63*u+leaf)
            for cut in range(3):
                center_cut=.14+cut*.30+.045*math.sin(panel_index*3+leaf+cut)
                torn-=.018*math.exp(-((u-center_cut)/(.014+.004*(cut%2)))**2)''')
marker="        grid('Spirit_Cape_'+name+'_broad_fold_'+str(leaf),page,36,30,garment,.0015,(1.2,1.6))"
addition='''
        # Sparse flat pulp filaments follow the hem, never fur or a regular fringe.
        for strand in range(7):
            edge_u=.08+.84*((strand*.61803398875+panel_index*.17+leaf*.11)%1)
            base=page(edge_u,1)
            along=(page(min(.999,edge_u+.005),1)-page(max(.001,edge_u-.005),1)).normalized()
            length=.004+.006*((strand*.41+leaf*.27)%1)
            def pulp(s,v,base=base,along=along,length=length,strand=strand):
                return base+Vector((.001*math.sin(strand)*v,.002*v*v,-length*v))+along*((s-.5)*.0011*(1-v))
            grid('Spirit_Cape_'+name+'_hem_fiber_'+str(leaf)+'_'+str(strand),pulp,1,3,garment,0,(.05,.1))

# Subtle wear on the original broad-panel free edges as well.
for obj in list(root.children):
    if obj.name.startswith('Spirit_Cape_') and len(obj.data.vertices)==45*49:
        for vertex in obj.data.vertices:
            row=vertex.index//45;u=(vertex.index%45)/44
            if row>=46:
                edge=((row-46)/2)**3
                wear=.002*math.sin(u*83)+.002*math.sin(u*147+.7)
                for cut in range(4):
                    wear+=.006*math.exp(-((u-(.12+cut*.23))/.017)**2)
                vertex.co.z+=wear*edge
'''
assert marker in code
code=code.replace(marker,marker+addition)
exec(compile(code,str(ROOT/'Tools/refine_spirit_garment.py'),'exec'))
