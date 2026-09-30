"""Layered worn paper cowl and subtly oval face; standalone V3 review asset."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
code=(ROOT/'Tools/model_turnaround_spirit.py').read_text(encoding='utf-8')
code=code.replace('TurnaroundSpiritModelV1','TurnaroundSpiritModelV3')
code=code.replace('face.scale=(.87,.92,.89)','face.scale=(.87,.92,.835)')
code=code.replace('(o.location.z-1.315)*.89','(o.location.z-1.315)*.835')
lining='''
# Face is 0.611 wide and 0.578 high: a subtle oval, with matching cowl clearance.
def inner_cowl(u,t,offset=0):
    a=math.tau*u
    opening=.94+.035*math.sin(3*a+.4)+.018*math.sin(7*a)
    phi=opening+(2.38-opening)*t
    sx=math.sin(phi);cy=math.cos(phi)
    p=Vector(((.313+offset)*sx*math.cos(a),-.085-(.298+offset)*cy,1.315+(.298+offset)*sx*math.sin(a)))
    radial=Vector((sx*math.cos(a),-cy,sx*math.sin(a)))
    p+=radial*(.002*math.sin(9*a+8*t)*math.sin(math.pi*t)+.002*(1-t)**8)
    return p
grid('Spirit_Hood_inner_face_wrap',inner_cowl,160,38,inside,.0018,(2,1.5))

# Three broad, thin overlapping paper edges; irregular wear, not fabric piping.
for layer in range(3):
    def edge_start(u,layer=layer):
        a=math.tau*u
        rag=.0025*math.sin(37*a+layer)+.0015*math.sin(61*a+.8*layer)
        for k in range(7):
            center=math.tau*((k*.137+layer*.057+.031)%1)
            d=math.atan2(math.sin(a-center),math.cos(a-center))
            rag+=.012*math.exp(-(d/.028)**2)
        return .014+layer*.046+.009*math.sin(4*a+layer*.9)+rag
    def cowl_page(u,v,layer=layer):
        start=edge_start(u,layer)
        t=start+v*.105
        lift=.0015+layer*.0028+.0025*(1-v)**3
        return inner_cowl(u,t,lift)
    grid('Spirit_Hood_inner_paper_layer_'+str(layer),cowl_page,240,12,paper,.0011,(2,.3))
    for k in range(22):
        u=(k*.61803398875+layer*.037)%1
        t=edge_start(u,layer)
        p=inner_cowl(u,t,.004+layer*.0028)
        tangent=(inner_cowl(u+.0008,t,.004+layer*.0028)-p).normalized()
        inward=(inner_cowl(u,t+.006,.004+layer*.0028)-p).normalized()
        length=.0025+.003*(.5+.5*math.sin(k*5.3+layer))
        width=.00055
        tip=p-inward*length+tangent*.0007*math.sin(k)
        mesh('Spirit_Hood_edge_pulp_%d_%d'%(layer,k),[p-tangent*width,p+tangent*width,tip],[(0,1,2)],paper,thickness=.00035)

'''
marker='for o in parts:o.parent=root'
assert marker in code
code=code.replace(marker,lining+marker)
exec(compile(code,str(ROOT/'Tools/model_turnaround_spirit.py'),'exec'))
