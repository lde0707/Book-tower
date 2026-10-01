"""Package actual Unity renders for review; no generated artwork or colour enhancement."""
from pathlib import Path
from PIL import Image, ImageOps, ImageDraw
import argparse

parser=argparse.ArgumentParser()
parser.add_argument('folder',type=Path)
args=parser.parse_args()
folder=args.folder
thumb=(640,360)
sheet=Image.new('RGB',(2560,1176),'#161513')
draw=ImageDraw.Draw(sheet)
for row,room in enumerate(('F0','1A','1B')):
    for col,yaw in enumerate((0,90,180,270)):
        with Image.open(folder/f'{room}-{yaw}.png') as source:
            im=source.convert('RGB').resize(thumb,Image.Resampling.LANCZOS)
        sheet.paste(im,(col*640,row*392+28))
        draw.text((col*640+12,row*392+8),f'{room} / map yaw {yaw}',fill='white')
sheet.save(folder/'three-room-four-views.jpg',quality=92)
grey=ImageOps.grayscale(sheet)
grey.resize((1280,588),Image.Resampling.LANCZOS).save(folder/'composition-grayscale.jpg',quality=92)
before=folder.parent/'03-Lighting'/'After'
compare=Image.new('RGB',(1600,1440),'#161513')
d=ImageDraw.Draw(compare)
for row,(room,old) in enumerate((('F0','01-F0-start'),('1A','03-1A-stage'),('1B','05-1B-entry'))):
    for col,path in enumerate((before/f'{old}.png',folder/f'{room}-0.png')):
        if not path.exists():continue
        with Image.open(path) as source:
            compare.paste(source.convert('RGB').resize((800,450),Image.Resampling.LANCZOS),(col*800,row*480+26))
        d.text((col*800+12,row*480+7),f'{room}: '+('stage 03' if col==0 else 'final pass'),fill='white')
compare.save(folder/'before-after.jpg',quality=92)
motion=folder/'Motion'
sources=sorted(motion.glob('frame-*.png'))
if sources:
    frames=[]
    for p in sources:
        with Image.open(p) as im:frames.append(im.convert('RGB').resize((800,450)))
    frames[0].save(motion/'sixty-second-observation.gif',save_all=True,append_images=frames[1:],duration=1000,loop=0)
    for f in frames:f.close()
for clip in (folder/'Standalone').glob('*-clip') if (folder/'Standalone').exists() else []:
    sources=sorted(clip.glob('frame-*.png'))
    frames=[]
    for p in sources:
        with Image.open(p) as im:frames.append(im.convert('RGB').resize((800,450),Image.Resampling.LANCZOS))
    if frames:
        frames[0].save(clip.parent/(clip.name+'.gif'),save_all=True,append_images=frames[1:],duration=83,loop=0)
    for f in frames:f.close()
print('Packaged real Unity images in',folder)
