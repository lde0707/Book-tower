"""Original paper-spirit mesh matching Docs/ArtReview/reference/paper-spirit.png.
Generate Unity mesh data and a portable, material-grouped OBJ (metres, Y up).
"""
import json, math
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
OUT=Path('Assets/_BookTower/Art/PaperSpirit'); OUT.mkdir(parents=True,exist_ok=True)
parts=[]
def grid(name,mat,fn,nu,nv,thick=0):
    verts=[]; uv=[]; tri=[]
    layers=2 if thick else 1
    for layer in range(layers):
        for j in range(nv+1):
            for i in range(nu+1):
                u,v=i/nu,j/nv
                point=np.array(fn(u,v),float)
                if thick:
                    du=np.array(fn(min(1,u+.0001),v))-np.array(fn(max(0,u-.0001),v))
                    dv=np.array(fn(u,min(1,v+.0001)))-np.array(fn(u,max(0,v-.0001)))
                    normal=np.cross(du,dv); normal/=max(1e-12,np.linalg.norm(normal))
                    point+=normal*((layer*2-1)*thick/2)
                verts.append(point.tolist()); uv.append([u,v])
    n=(nu+1)*(nv+1)
    for layer in range(layers):
        for j in range(nv):
            for i in range(nu):
                a=layer*n+j*(nu+1)+i; b=a+nu+1
                ts=[a,b,a+1,a+1,b,b+1]
                if layer: ts=[ts[k] for k in [2,1,0,5,4,3]]
                tri.extend(ts)
    if thick:
        rim=list(range(nu+1))+[j*(nu+1)+nu for j in range(1,nv+1)]+[nv*(nu+1)+i for i in range(nu-1,-1,-1)]+[j*(nu+1) for j in range(nv-1,0,-1)]
        for a,b in zip(rim,rim[1:]+rim[:1]): tri.extend([a,b,a+n,b,b+n,a+n])
    parts.append(dict(name=name,material=mat,positions=[dict(zip('xyz',p)) for p in verts],uv=[dict(zip('xy',p)) for p in uv],triangles=tri))
def sphere(center,scale):
    def f(u,v):
        a=2*math.pi*u; t=math.pi*v
        wrinkle=1+.005*math.sin(a*13+v*31)*math.sin(t)**2+.003*math.sin(a*23-v*43)*math.sin(t)**2
        return (center[0]+scale[0]*math.sin(t)*math.cos(a)*wrinkle,center[1]-scale[1]*math.cos(t),center[2]+scale[2]*math.sin(t)*math.sin(a)*wrinkle)
    return f
grid('Head','Paper',sphere((0,1.075,0),(.298,.325,.265)),64,40)
# Body is a swept, curled teardrop; the reference's tail bends sideways and back.
keys=[0,.07,.16,.28,.44,.62,.80,1]
ys=[.045,.075,.14,.21,.34,.49,.66,.81]
xs=[.12,.17,.15,.075,-.04,-.05,-.015,0]
rs=[0,.012,.029,.065,.14,.22,.20,.125]
def smooth(values,v):
    slopes=np.gradient(values,keys)
    i=min(len(keys)-2,max(0,int(np.searchsorted(keys,v)-1)))
    d=keys[i+1]-keys[i]; t=(v-keys[i])/d
    return (2*t**3-3*t*t+1)*values[i]+(t**3-2*t*t+t)*d*slopes[i]+(-2*t**3+3*t*t)*values[i+1]+(t**3-t*t)*d*slopes[i+1]
def body(u,v,offset=0):
    a=2*math.pi*u
    y=smooth(ys,v); x=smooth(xs,v); r=max(0,smooth(rs,v))+offset
    fold=1+.017*math.sin(a*5+v*9)
    return x+r*math.cos(a)*fold,y,.75*r*math.sin(a)*fold
# cap at neck beneath head and scarf
grid('CurledBody','Paper',lambda u,v: body(u,v),64,48)
# Real overlapping sheet geometry rather than painted folds. Helical hems visible all around.
for k in range(1):
    def sheet(u,v,k=k):
        angle=.75+u*.98
        hem=.12+.62*u+.008*math.sin(u*23)
        h=hem*(1-v)+.965*v
        return body(angle,h,.008+ .007*math.sin(math.pi*v)**2)
    grid('OverlappingSheet'+str(k),'Paper',sheet,56,14,.003)
# Two ink eyes lie on the rounded forehead.
for side in [-1,1]:
    grid('InkEye'+str(side),'Ink',sphere((side*.103,1.10,.249),(.023,.039,.007)),20,14)
# Neck ribbon, flattened cylindrical band.
def collar(u,v):
    a=2*math.pi*u; r=.147+.005*math.sin(a*3)
    return r*math.cos(a),.78+v*.045+.005*math.sin(a),r*math.sin(a)
grid('NeckRibbon','Ribbon',collar,64,4,.003)
for side in [-1,1]:
    def tail(u,v,side=side):
        w=.044*(1+.2*math.sin(v*6))
        notch=.045*(1-abs(2*u-1))*v**12
        return side*(.12+.32*v+.014*math.sin(v*8)), .80-.19*v-.085*v*v+(u-.5)*2*w+notch, -.025-.06*math.sin(v*math.pi)+.04*(u-.5)*math.sin(v*6)
    grid('RibbonTail'+str(side),'Ribbon',tail,8,24,.004)
# Small soft knot at the back.
grid('RibbonKnot','Ribbon',sphere((0,.80,-.14),(.060,.037,.029)),20,12)
(OUT/'paper-spirit.json').write_text(json.dumps(dict(parts=parts),separators=(',',':')),encoding='utf-8')
lines=['# Paper Spirit - original geometry; metres, Y up','mtllib paper-spirit.mtl']; base=1
for p in parts:
    lines.extend(['o '+p['name'],'usemtl '+p['material'],'s 1'])
    for q in p['positions']: lines.append('v %.7f %.7f %.7f'%(-q['x'],q['y'],q['z']))
    for q in p['uv']: lines.append('vt %.7f %.7f'%(q['x'],q['y']))
    for i in range(0,len(p['triangles']),3):
        inds=[base+p['triangles'][i+k] for k in [2,1,0]]
        lines.append('f '+' '.join(f'{n}/{n}' for n in inds))
    base+=len(p['positions'])
(OUT/'paper-spirit.obj').write_text('\n'.join(lines),encoding='utf-8')
(OUT/'paper-spirit.mtl').write_text('newmtl Paper\nKd 0.94 0.90 0.81\nmap_Kd PaperFiber.png\nNs 0\n\nnewmtl Ribbon\nKd 0.63 0.25 0.16\nmap_Kd PaperFiber.png\nNs 0\n\nnewmtl Ink\nKd 0.12 0.10 0.075\nNs 0\n',encoding='utf-8')
# Deterministic fibrous paper, with a separate height-derived normal map.
rng=np.random.default_rng(924); n=1024
base=rng.normal(232,2,(n,n)); fine=Image.fromarray(np.uint8(np.clip(base,0,255))).convert('RGB'); draw=ImageDraw.Draw(fine)
for _ in range(23000):
    x,y=rng.integers(0,n,2); length=int(rng.integers(3,20)); angle=rng.uniform(0,math.pi)
    tone=int(rng.integers(205,250)); dx=int(math.cos(angle)*length); dy=int(math.sin(angle)*length)
    draw.line((int(x),int(y),int(x+dx),int(y+dy)),fill=(tone,tone,tone),width=1)
fine=fine.filter(ImageFilter.GaussianBlur(.35)); fine.save(OUT/'PaperFiber.png')
h=np.asarray(fine.convert('L'),float)/255
x=(np.roll(h,-1,1)-np.roll(h,1,1))*2; y=(np.roll(h,-1,0)-np.roll(h,1,0))*2
norm=np.stack([-x,-y,np.ones_like(x)],axis=-1); norm/=np.linalg.norm(norm,axis=-1,keepdims=True)
Image.fromarray(np.uint8((norm*.5+.5)*255)).save(OUT/'PaperFiberNormal.png')
print('Parts:',len(parts),'Vertices:',sum(len(p['positions']) for p in parts),'Triangles:',sum(len(p['triangles'])//3 for p in parts))
