import json, sys
from PIL import Image, ImageDraw
import room_preview as RP
d = json.load(open(sys.argv[1], encoding="utf-8"))
RP.set_palette({e["key"]: e["hex"] for e in d.get("palette", [])})
W, H = 640, 420
areas = d["areas"]
cols = 4
img = Image.new("RGB", (cols * W, ((len(areas) + cols - 1) // cols) * (H + 20)), (255, 255, 255))
dr = ImageDraw.Draw(img)
for i, a in enumerate(areas):
    im, _ = RP.render_area(a, W, H)
    x, y = (i % cols) * W, (i // cols) * (H + 20)
    img.paste(im, (x, y + 20))
    n = sum(1 for o in a["objects"] if o["name"].startswith("장식_"))
    dr.text((x + 6, y + 4), f"{a['id']}  deco {n}", fill=(40, 40, 40))
img.save(sys.argv[2])
if len(sys.argv) > 3:
    a = next(x for x in areas if x["id"] == sys.argv[3])
    RP.render_area(a, 1600, 1050)[0].save(sys.argv[4])
