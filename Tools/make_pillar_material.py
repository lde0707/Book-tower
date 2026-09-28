from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

out=Path('Assets/_BookTower/Textures/BookSpinePillar');out.mkdir(parents=True,exist_ok=True)
rng=np.random.default_rng(2719);n=1024
def noise(s):
    return np.asarray(Image.fromarray(rng.integers(0,256,(s,s),dtype=np.uint8)).resize((n,n),Image.Resampling.BICUBIC),dtype=float)/255-.5
y,x=np.mgrid[:n,:n];step=32;cells=n//step
points=rng.uniform(.15,.85,(cells,cells,2))
first=np.full((n,n),1e9);second=first.copy()
for oy in (-1,0,1):
    for ox in (-1,0,1):
        cy=y//step+oy;cx=x//step+ox;p=points[cy%cells,cx%cells]
        dist=((cx+p[:,:,0])*step-x)**2+((cy+p[:,:,1])*step-y)**2
        second=np.minimum(second,np.maximum(first,dist));first=np.minimum(first,dist)
crack=np.exp(-(np.sqrt(second)-np.sqrt(first))*1.7)
grain=noise(512);broad=noise(16);height=.5+.10*grain+.12*noise(100)-.20*crack
base=np.stack([100+30*broad+24*grain-35*crack,36+13*broad+12*grain-16*crack,21+10*broad+8*grain-9*crack],-1)
# Fine worn gold botanical tooling, wrapped four times around the circumference.
mask=Image.new('L',(n,n));d=ImageDraw.Draw(mask)
for cx in (128,384,640,896):
    for cy in (90,285,480,700,910):
        d.line([(cx-5,cy+53),(cx+1,cy),(cx-2,cy-54)],fill=210,width=2)
        for k in range(5):
            yy=cy+35-k*17
            for side in (-1,1):
                d.polygon([(cx,yy+4),(cx+side*25,yy-12),(cx+side*23,yy-1),(cx+side*10,yy+6)],fill=225)
        d.polygon([(cx-2,cy-40),(cx-9,cy-54),(cx-2,cy-68),(cx+5,cy-54)],fill=225)
gold=np.asarray(mask)/255*np.clip(.65+noise(190)*2,0,1)
base=base*(1-gold[:,:,None])+np.array([185,131,55])*gold[:,:,None]
Image.fromarray(np.uint8(np.clip(base,0,255))).save(out/'Leather_Base.png')
dy,dx=np.gradient(height);v=np.stack([-dx*4,-dy*4,np.ones_like(dx)],-1);v/=np.linalg.norm(v,axis=-1,keepdims=True)
Image.fromarray(np.uint8((v*.5+.5)*255)).save(out/'Leather_Normal.png')
metal=np.zeros((n,n,4),dtype=np.uint8);metal[:,:,:3]=np.uint8(gold[:,:,None]*180);metal[:,:,3]=np.uint8(55+gold*55)
Image.fromarray(metal).save(out/'Leather_Metallic.png')
print('Created leather albedo, normal and metallic/smoothness maps')
