"""Original crayon murals for the folded storybook room; no downloaded assets."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter
import numpy as np
import math
OUT=Path('Assets/_BookTower/Textures/Storybook');OUT.mkdir(parents=True,exist_ok=True)
rng=np.random.default_rng(142)
N=1024
for animal,color in [('Rabbit',(208,108,82)),('Fox',(205,99,66)),('Bird',(61,143,139))]:
 im=Image.new('RGBA',(N,N));d=ImageDraw.Draw(im)
 if animal=='Rabbit':
  d.ellipse((220,460,715,875),fill=color+(235,));d.ellipse((505,340,820,620),fill=color+(235,));d.ellipse((565,65,650,415),fill=color+(235,));d.ellipse((680,105,770,420),fill=color+(235,));d.ellipse((150,670,295,800),fill=color+(235,));d.ellipse((530,825,800,900),fill=color+(235,));d.ellipse((725,423,750,460),fill=(68,53,43,255));d.ellipse((792,490,836,526),fill=(110,75,54,255))
 elif animal=='Fox':
  d.polygon([(190,90),(405,250),(650,250),(850,90),(860,460),(930,510),(820,625),(790,870),(280,870),(230,610),(140,500),(205,450)],fill=color+(240,));d.polygon([(198,485),(500,580),(842,485),(760,670),(525,720),(292,655)],fill=(242,208,149,250));d.polygon([(240,175),(350,275),(254,355)],fill=(244,200,138,250));d.polygon([(785,175),(690,275),(790,355)],fill=(244,200,138,250));d.ellipse((350,410,378,462),fill=(67,53,45,255));d.ellipse((680,410,708,462),fill=(67,53,45,255));d.polygon([(495,582),(548,582),(522,614)],fill=(67,53,45,255))
 else:
  d.ellipse((220,400,740,825),fill=color+(240,));d.ellipse((470,195,760,515),fill=color+(240,));d.polygon([(245,590),(88,360),(160,675),(305,753)],fill=color+(240,));d.polygon([(715,323),(860,350),(725,379)],fill=(223,151,61,255));d.ellipse((654,272,682,314),fill=(66,53,43,255));d.arc((275,410,660,735),30,290,fill=(240,203,130,250),width=22)
  for x in [403,564]:d.line([(x,788),(x,914),(x-60,946),(x,914),(x+60,946)],fill=(218,138,60,255),width=20)
 a=np.array(im); mask=a[:,:,3]>0
 # Broken wax marks and scattered tiny paper pores.
 noise=rng.random((N,N));a[:,:,3]=np.where(mask,np.clip(a[:,:,3].astype(float)*(0.48+noise*.52),0,255),0)
 a[noise<.17,3]=0
 Image.fromarray(a).save(OUT/(animal+'.png'))
 # Solid rabbit for two complementary shadow masks.
 if animal=='Rabbit':
  shadow=np.array(im);shadow[:,:,:3]=[58,43,35]
  for half in range(2):
   sh=shadow.copy()
   if half==0:sh[:,512:,3]=0
   else:sh[:,:512,3]=0
   Image.fromarray(sh).save(OUT/('RabbitShadow'+str(half)+'.png'))
print('Storybook murals generated')

