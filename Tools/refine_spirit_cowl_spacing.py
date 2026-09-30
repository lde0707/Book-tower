"""Spread the paper cowl layers and sweep the lower opening behind the jaw.

Blender --background --python Tools/refine_spirit_cowl_spacing.py -- model|bake
"""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
code=(ROOT/'Tools/refine_spirit_face_lining.py').read_text(encoding='utf-8')
code=code.replace('TurnaroundSpiritModelV3','TurnaroundSpiritModelV4')
# Opening moves toward the back over the lower cheek, most strongly at the chin.
# Change the underlying cowl too so its original border cannot remain under the jaw.
code=code.replace(
    'opening=.94+.035*math.sin(3*a+.4)+.018*math.sin(7*a)',
    'lower=max(0,-math.sin(a))\n    opening=.94+.035*math.sin(3*a+.4)+.018*math.sin(7*a)+.52*lower**1.7')
# Broader exposed paper bands, still overlapping enough to avoid open seams.
code=code.replace('.014+layer*.046','.018+layer*.115')
code=code.replace('t=start+v*.105','t=start+v*.18')
exec(compile(code,str(ROOT/'Tools/refine_spirit_face_lining.py'),'exec'))
