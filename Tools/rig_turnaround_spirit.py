"""Rig V6 with connected limbs and a distinct run cycle; export to validation project."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
code=(ROOT/'Tools/rig_hood_spirit.py').read_text(encoding='utf-8')
code=code.replace('ArtSource/PaperHoodSpiritV1','ArtSource/TurnaroundSpiritModelV6')
code=code.replace('Docs/ArtReview/SpiritRedesignV1/AnimationV1','Docs/ArtReview/TurnaroundSpiritModelV6/Animation')
code=code.replace("ROOT/'Assets/_BookTower/Art/PaperHoodSpirit'","ROOT/'Logs/F0ArtValidation/Assets/_BookTower/Art/PaperHoodSpirit'")
code=code.replace('def weights(name,p):','''def weights(name,p):
    if 'LegFoot' in name:
        side='L' if p.x<0 else 'R'
        if p.z>.23:return chain(p,'Leg'+side,'Knee'+side,.36,.23)
        return chain(p,'Knee'+side,'Foot'+side,.15,.075)
    if 'ArmHand' in name:
        side='L' if p.x<0 else 'R';w=smooth((abs(p.x)-.14)/.11)
        return {'Body':1-w,'Arm'+side:w}
    if 'Scarf' in name:
        if 'tail' in name:return chain(p,'Body','CapeBackTip',1.03,.76)
        return {'Body':.88,'Head':.12}
    if 'inner' in name and 'Hood' in name:return {'Head':1}
    if 'rounded_body' in name:return {'Body':1}
''')
code=code.replace("w=.75*smooth((p.y-.22)/.40)","w=.75*smooth((p.z-1.78)/.40)")
code=code.replace("('HoodTip',(0,.27,1.68),'Head')","('HoodTip',(0,.10,1.82),'Head')")
code=code.replace("for name,pos,parent in spec:","""for side,x in [('L',-.12),('R',.12)]:
    spec.extend([('Leg'+side,(x,0,.46),'Body'),('Knee'+side,(x,-.015,.24),'Leg'+side),('Foot'+side,(x,-.025,.075),'Knee'+side)])
for name,pos,parent in spec:""")
code=code.replace("('Move',.9,True)","('Move',1.1,True),('Run',.65,True)")
code=code.replace("        blink(t,", """        # Connected leg joints and distinct walk/run gait, keyed at 60 fps.
        if name in ('Move','Run'):
            running=name=='Run';stride=24 if running else 13
            rot('Body',x=9 if running else 4,y=(3 if running else 1.5)*sw)
            rot('Head',x=-5 if running else -2)
            move('MotionRoot',z=(.018 if running else .008)*(1-math.cos(phase*2)))
            for side,sign in [('L',1),('R',-1)]:
                swing=sw*sign;lift=max(0,swing)
                rot('Leg'+side,x=stride*swing)
                rot('Knee'+side,x=-(24 if running else 12)*lift)
                rot('Foot'+side,x=-stride*swing*.35)
                rot('Arm'+side,x=-(22 if running else 10)*swing,y=(-8 if side=='L' else 8))
            rot('HoodTip',x=-5+(9 if running else 4)*late)
            for k,g in enumerate(['CapeL','CapeR','CapeBack']):
                rot(g,x=-3+(4 if running else 2)*late)
                rot(g+'Tip',x=-4+(7 if running else 3)*math.sin(phase-.6-k*.3))
        elif name=='JumpRise':
            for side in ('L','R'):rot('Leg'+side,x=-12*env);rot('Knee'+side,x=-24*env);rot('Foot'+side,x=15*env)
        elif name in ('Fall','Glide'):
            for side in ('L','R'):rot('Knee'+side,x=-10);rot('Foot'+side,x=8)
        elif name=='Land':
            for side in ('L','R'):rot('Leg'+side,x=12*env);rot('Knee'+side,x=-22*env)
        blink(t,""")
exec(compile(code,str(ROOT/'Tools/rig_hood_spirit.py'),'exec'))
