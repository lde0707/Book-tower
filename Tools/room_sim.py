# -*- coding: utf-8 -*-
"""룸 전환 시뮬레이션.

RoomManager.Pick 의 판정 규칙을 그대로 파이썬으로 옮겨서, 유니티를 켜지 않고
"플레이어가 경로를 걸을 때 방이 제대로 바뀌는가"를 확인한다.

    python3 Tools/room_sim.py

이 시뮬레이터가 실제로 잡아낸 것
  · 2A→2B 경사로를 오르는 도중에 엉뚱한 2E 가 선택됨
    → 경사로에 fromId/toId 를 명시하고, 경사로 판정을 방 판정보다 먼저 하도록 수정
  · 2F 가 한 번도 활성화되지 않음
  · F0 와 1C 가 높이 4 차이로 XZ 겹침 → 어느 방인지 판정이 흔들림
    → 1C 를 나선 바깥으로 8 밀어냄

C# 쪽 Pick 규칙을 바꾸면 이 파일의 pick() 도 같이 바꿔야 한다.
둘이 어긋나면 이 검사는 아무것도 보장하지 못한다.
"""
import json, math
L = json.load(open('Assets/_BookTower/Level/level_layout.json', encoding='utf-8'))

PAD = 1.0
def room_bounds(a):
    px, py, pz = a['pos']['x'], a['pos']['y'], a['pos']['z']
    lo=[-a['size']['x']/2,-0.1,-a['size']['z']/2]; hi=[a['size']['x']/2,0.1,a['size']['z']/2]
    if a['backWall']>0.01: hi[1]=max(hi[1], a['backWall'])
    for o in a['objects']:
        for i,ax in enumerate('xyz'):
            c=o['pos'][ax]; h=o['scale'][ax]/2
            lo[i]=min(lo[i],c-h); hi[i]=max(hi[i],c+h)
    c=[(lo[i]+hi[i])/2 for i in range(3)]; s=[hi[i]-lo[i]+PAD for i in range(3)]
    return ([px+c[0],py+c[1],pz+c[2]], s, py)

ROOMS={a['id']: room_bounds(a) for a in L['areas']}
RAMPS=L['ramps']

def contains(rid,p):
    c,s,fy=ROOMS[rid]
    mn=[c[i]-s[i]/2 for i in range(3)]; mx=[c[i]+s[i]/2 for i in range(3)]
    if not (mn[0]<=p[0]<=mx[0] and mn[2]<=p[2]<=mx[2]): return False
    return fy-2.5 < p[1] < mx[1]+5

def seg_dist(p,a,b):
    ab=[b[i]-a[i] for i in range(3)]; l2=sum(v*v for v in ab)
    t=0.0 if l2<1e-6 else max(0.0,min(1.0,sum((p[i]-a[i])*ab[i] for i in range(3))/l2))
    q=[a[i]+ab[i]*t for i in range(3)]
    return math.dist(p,q), t

RAMP_REACH=4.5
def pick(p,cur):
    best=None; bd=1e9; bt=0
    for r in RAMPS:
        a=[r['from']['x'],r['from']['y'],r['from']['z']]; b=[r['to']['x'],r['to']['y'],r['to']['z']]
        d,t=seg_dist(p,a,b)
        if d<bd: bd,best,bt=d,r,t
    if best and bd<=RAMP_REACH:
        return best['fromId'] if bt<0.5 else best['toId']
    inside=None; gap=1e9
    for rid in ROOMS:
        if not contains(rid,p): continue
        g=abs(p[1]-ROOMS[rid][2])
        if g<gap: gap=g; inside=rid
    return inside if inside else cur

ROUTE=["F0","1A","1B","1C","1D","1E","2A","2B","2C","2D","2E","2F","3A","3B","3C"]
byid={a['id']:a for a in L['areas']}
ramps={r['name']:r for r in RAMPS}
def lerp(a,b,t): return [a[i]+(b[i]-a[i])*t for i in range(3)]

cur=None; seq=[]; gaps=[]
for i,rid in enumerate(ROUTE):
    a=byid[rid]
    path=[[a['pos']['x'],a['pos']['y']+1,a['pos']['z']]]
    # 방 네 모서리도 밟아본다 — 큰 방 구석에서 판정이 풀리는지 확인
    for sx,sz in ((-.45,-.45),(.45,-.45),(.45,.45),(-.45,.45)):
        path.append([a['pos']['x']+a['size']['x']*sx, a['pos']['y']+1, a['pos']['z']+a['size']['z']*sz])
    path.append([a['pos']['x'],a['pos']['y']+1,a['pos']['z']])
    if i+1<len(ROUTE):
        r=ramps["연결_%s→%s"%(rid,ROUTE[i+1])]
        f=[r['from']['x'],r['from']['y']+1,r['from']['z']]; t=[r['to']['x'],r['to']['y']+1,r['to']['z']]
        path.append(f)
        for k in range(1,41): path.append(lerp(f,t,k/40))
    for p in path:
        n=pick(p,cur)
        if n!=cur: seq.append(n); cur=n
        if n!=rid and n not in (ROUTE[i+1] if i+1<len(ROUTE) else rid,):
            gaps.append((rid,n,[round(v,1) for v in p]))

print("전환 순서:")
print("  " + " → ".join(seq))
print("\n총 전환 %d회 / 기대 %d회" % (len(seq), len(ROUTE)))
print("검사1 순서   :", "✓ 경로대로 정확히" if seq==ROUTE else "✗ 불일치")
missed=[r for r in ROUTE if r not in seq]
print("검사2 누락   :", "✓ 없음" if not missed else "✗ "+str(missed))
print("검사3 오판정 :", "✓ 없음" if not gaps else "✗ %d건  예: %s"%(len(gaps),gaps[:3]))

# 방 부피 XZ 겹침
print("\n방 부피 겹침(같은 층끼리만 문제):")
ids=list(ROOMS); ov=0
for i in range(len(ids)):
    for j in range(i+1,len(ids)):
        (c1,s1,f1),(c2,s2,f2)=ROOMS[ids[i]],ROOMS[ids[j]]
        xz = all(abs(c1[k]-c2[k])<(s1[k]+s2[k])/2 for k in (0,2))
        if xz:
            print("  ! %s(바닥 %.0f) ↔ %s(바닥 %.0f)  높이차 %.0f"%(ids[i],f1,ids[j],f2,abs(f1-f2))); ov+=1
print("  겹침 %d쌍 — 높이차로 구분 가능하면 문제 없음"%ov)
