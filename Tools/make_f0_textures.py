from pathlib import Path
import numpy as np
from PIL import Image,ImageDraw,ImageFilter
out=Path('Assets/_BookTower/Textures/F0Reference');out.mkdir(parents=True,exist_ok=True)
rng=np.random.default_rng(617)
n=1024

def field(size):
 a=Image.fromarray(rng.integers(0,256,(size,size),dtype=np.uint8)).resize((n,n),Image.Resampling.BICUBIC)
 return np.asarray(a,dtype=np.float32)/255-.5

def make(name,kind):
 h=.5+field(9)*.28+field(35)*.18+field(140)*.12+field(512)*.06
 fibers=Image.new('L',(n,n),128);d=ImageDraw.Draw(fibers)
 for i in range(6500 if kind=='paper' else 2200):
  x,y=rng.uniform(0,n,2);a=rng.uniform(0,np.pi*2);l=rng.uniform(2,24)
  d.line((x,y,x+np.cos(a)*l,y+np.sin(a)*l),fill=int(rng.uniform(75,180)),width=1)
 f=np.asarray(fibers,dtype=np.float32)/255-.5
 if kind=='paper':
  cr=Image.new('L',(n,n),128);d=ImageDraw.Draw(cr)
  for i in range(230):
   x,y=rng.uniform(-30,n+30,2);pts=[(x,y)]
   for j in range(int(rng.integers(3,8))):
    x+=rng.uniform(-30,30);y+=rng.uniform(5,32);pts.append((x,y))
   d.line(pts,fill=int(rng.uniform(65,110)),width=int(rng.integers(1,3)))
  wrinkle=np.asarray(cr.filter(ImageFilter.GaussianBlur(1.1)),dtype=np.float32)/255-.5
  h+=wrinkle*.5+f*.3
  v=np.clip(.90+(h-.5)*.20+f*.18,0,1)
 elif kind=='cover':
  h+=field(260)*.16+f*.22
  v=np.clip(.78+(h-.5)*.5,0,1)
 elif kind=='edge':
  lines=rng.uniform(-.10,.08,(n,1));h+=lines
  v=np.clip(.88+(h-.5)*.28+lines*.45,0,1)
 else:
  h+=f*.25;v=np.clip(.72+(h-.5)*.9,0,1)
 Image.fromarray(np.uint8(np.stack([v,v*.97,v*.89],-1)*255)).save(out/(name+'_Albedo.png'))
 dy,dx=np.gradient(h);normal=np.stack([-dx*7,-dy*7,np.ones_like(h)],-1);normal/=np.linalg.norm(normal,axis=-1,keepdims=True)
 Image.fromarray(np.uint8(np.clip(normal*.5+.5,0,1)*255)).save(out/(name+'_Normal.png'))
for name,kind in [('Paper','paper'),('Cover','cover'),('Edges','edge'),('Rope','rope')]:make(name,kind)
print('Created eight 1024px original albedo/normal texture maps')
