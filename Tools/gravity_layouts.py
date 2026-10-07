"""Build deterministic gravity routes; BFS proves shortest tilt counts, not fun."""
from collections import deque
from pathlib import Path
import json, random

AXES = [(0,-1),(-1,0),(0,1),(1,0)]
def cells(p, size):
    return {(p[0]+x,p[1]+y) for x in range(size) for y in range(size)}

def tilt(state, sizes, walls, direction):
    dx,dy=AXES[direction]; out=list(state)
    for i in sorted(range(len(out)),key=lambda i:out[i][0]*dx+out[i][1]*dy,reverse=True):
        occupied=walls|set().union(*(cells(p,sizes[j]) for j,p in enumerate(out) if j!=i))
        while True:
            p=(out[i][0]+dx,out[i][1]+dy)
            if cells(p,sizes[i])&occupied: break
            out[i]=p
    return tuple(out)

def search(start,sizes,walls,limit=12):
    seen={start:()}; queue=deque([start])
    while queue:
        state=queue.popleft(); path=seen[state]
        if len(path)>=limit:continue
        for d in range(4):
            nxt=tilt(state,sizes,walls,d)
            if nxt not in seen:
                seen[nxt]=path+(d,);queue.append(nxt)
        if len(seen)>40000:break
    return seen

def layout(room, family, target, seed):
    rng=random.Random(seed)
    w,h=(9,7) if family==0 else (13,11)
    sizes=[1] if family<2 else [1,1] if family==2 else [1,2] if family==3 else [1,1,2]
    boundary={(x,y) for x in range(w) for y in range(h) if x in (0,w-1) or y in (0,h-1)}
    if family==0:
        walls=boundary|{(4,y) for y in range(1,5)};start=((1,1),);goal=((7,1),)
        return package(room,family,w,h,walls,start,goal,sizes,search(start,sizes,walls)[goal])
    for attempt in range(150):
        walls=set(boundary)
        for j in range(4+family):
            x,y=rng.randrange(3,w-3),rng.randrange(2,h-2)
            length=rng.randrange(1,4);horizontal=rng.choice([True,False])
            walls|={(min(w-2,x+k) if horizontal else x,y if horizontal else min(h-2,y+k)) for k in range(length)}
        start=[]
        for size in sizes:
            options=[(x,1) for x in range(1,w-size) if not cells((x,1),size)&(walls|set().union(*(cells(p,sizes[k]) for k,p in enumerate(start))))]
            if not options:break
            start.append(rng.choice(options))
        if len(start)!=len(sizes):continue
        start=tuple(start); states=search(start,sizes,walls,target)
        choices=[(s,p) for s,p in states.items() if len(p)==target and p[-1]==0 and
                 all(s[i]!=start[i] for i in range(len(sizes))) and
                 all(tilt((s[i],),(sizes[i],),walls,0)==(s[i],) for i in range(len(sizes))) ]
        if choices:
            goal,path=rng.choice(choices)
            return package(room,family,w,h,walls,start,goal,sizes,path)
    raise RuntimeError(f'No suitable layout: {room}')

def package(room,family,w,h,walls,start,goal,sizes,path):
    return dict(room=room,family=family,width=w,height=h,walls=[dict(x=x,y=y) for x,y in sorted(walls)],
                cargo=[dict(x=p[0],y=p[1],size=sizes[i],gx=goal[i][0],gy=goal[i][1]) for i,p in enumerate(start)],route=list(path))

if __name__=='__main__':
    specs=[('F0',0,3),('1A',1,5),('1B',2,6),('1C',3,7),('1D',2,7),('1E',4,8),
           ('2A',1,5),('2B',3,8),('2C',4,9),('2D',3,9),('2E',2,9),('2F',4,8),
           ('3A',4,10),('3B',4,11),('3C',0,3),('HD',4,12)]
    result=[layout(room,family,target,6040+i) for i,(room,family,target) in enumerate(specs)]
    dest=Path('Assets/_BookTower/Design/GravityLayouts.json');dest.parent.mkdir(parents=True,exist_ok=True)
    dest.write_text(json.dumps(dict(layouts=result),ensure_ascii=False,indent=2),encoding='utf-8')
    for level in result:print(level['room'],len(level['route']),level['route'],flush=True)
