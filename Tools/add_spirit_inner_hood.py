"""Add a thin separate paper lining around the face; preserve the previous model.
Blender --background --python Tools/add_spirit_inner_hood.py -- model|bake
"""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
code=(ROOT/'Tools/model_turnaround_spirit.py').read_text(encoding='utf-8')
code=code.replace('TurnaroundSpiritModelV1','TurnaroundSpiritModelV2')
marker='for o in parts:o.parent=root'
lining='''
# A distinct paper cowl surrounds the face, beneath the large outer hat.
# The opening follows the face ellipsoid but has clearance for cheeks and eyelids.
def inner_cowl(u,t,offset=0):
    a=math.tau*u
    opening=.94+.035*math.sin(3*a+.4)+.018*math.sin(7*a)
    phi=opening+(2.38-opening)*t
    sx=math.sin(phi);cy=math.cos(phi)
    p=Vector(((.313+offset)*sx*math.cos(a),-.085-(.298+offset)*cy,1.315+(.317+offset)*sx*math.sin(a)))
    radial=Vector((sx*math.cos(a),-cy,sx*math.sin(a)))
    crease=.0025*math.sin(9*a+8*t)*math.sin(math.pi*t)
    # Slightly lifted irregular paper lip around the opening, not a thick fabric tube.
    lip=.004*(1-t)**8*(.6+.4*math.sin(5*a+.3))
    p+=radial*(crease+lip)
    return p
grid('Spirit_Hood_inner_face_wrap',inner_cowl,112,38,inside,.0025,(2,1.5))
def lining_lip(u,v):
    return inner_cowl(u,.027*v,.0015+.0015*math.sin(math.pi*v))
grid('Spirit_Hood_inner_turned_edge',lining_lip,112,6,paper,.0015,(2,.18))

'''
assert marker in code
code=code.replace(marker,lining+marker)
exec(compile(code,str(ROOT/'Tools/model_turnaround_spirit.py'),'exec'))
