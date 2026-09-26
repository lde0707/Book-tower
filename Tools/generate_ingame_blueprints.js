const fs = require('fs');
const path = require('path');

const root = path.resolve(__dirname, '..');
const src = path.join(root, 'Docs', 'ArtReview', 'RoomRedesignV2');
const out = path.join(root, 'Docs', 'Blueprints', 'RoomRedesignV2');
fs.mkdirSync(out, { recursive: true });

const rawRooms = JSON.parse(fs.readFileSync(path.join(src, 'rooms.json'), 'utf8'));
const interactions = JSON.parse(fs.readFileSync(path.join(src, 'interaction-layout.json'), 'utf8'));
const cameras = JSON.parse(fs.readFileSync(path.join(src, 'camera-layout.json'), 'utf8'));
const roomById = new Map(rawRooms.map(r => [r[0], r]));
const interactionById = new Map(interactions.map(r => [r.id, r]));
const cameraById = new Map(cameras.map(r => [r.id, r]));

const order = ['F0','1A','1B','1C','1D','1E','2A','2B','2C','2D','2E','2F','3A','3B','3C','HD'];
const index = Object.fromEntries(order.map((id,i)=>[id,i+1]));
const world = {
  F0:[0,0,-220], '1A':[100,4,-180], '1B':[170,8,-100], '1C':[170,12,0], '1D':[140,17,90], '1E':[70,22,160],
  '2A':[-30,28,180], '2B':[-120,34,140], '2C':[-180,40,60], '2D':[-180,46,-30], '2E':[-140,52,-120], '2F':[-50,58,-205],
  '3A':[-10,66,-110], '3B':[-10,72,-10], '3C':[-10,80,100], HD:[200,66,-200]
};
const silhouettes = {
  F0:'oval', '1A':'fan', '1B':'crescent', '1C':'bowtie', '1D':'branch', '1E':'rings',
  '2A':'loom', '2B':'horseshoe', '2C':'pinwheel', '2D':'s', '2E':'spiral', '2F':'nest',
  '3A':'rosette7', '3B':'trefoil', '3C':'bud', HD:'figure8'
};
const mechanics = {
  F0:['걸쇠 조각 A/B','리본 풀리','중앙 압력판'], '1A':['차광 날개 3','접이판 2','관측대 2','목표 홈'],
  '1B':['권양기 2','접이 다리 2','차광판','안전섬 3'], '1C':['벨로즈 2','고정 쐐기 2','반사잎 3','수광 꽃 3'],
  '1D':['먼지 생물 5','종 3','기록 조각 3','비교대 2'], '1E':['종이추 3','걸이 홈 5','모빌 가지 3','인장판'],
  '2A':['기억 주름 3','고정핀 3','발판 2','리본 다리 4'], '2B':['실루엣 소품 4','가림막 2','비교 투영대 2','무대 날개'],
  '2C':['지도 꽃잎 4','중심핀','표식 4','접기 잠금 2'], '2D':['분기판 3','기록 5','비교대 3','정지 손잡이'],
  '2E':['사건섬 5','연결 리본 5','재생 종 5','중앙 잠금'], '2F':['바람막이 2','종이등','안전 둥지 3','기억 주름'],
  '3A':['기록 7','비교대 3','기록 홈 7','왜상면 12'], '3B':['장치 A/B/C','반사면 2','고정추 2','광축'],
  '3C':['조리개 날개 3','잠금 3','승강 책갈피 2','역광 등대'], HD:['중첩 책 3','접지 손잡이 3','대응 표식 3','규모 문 3']
};
const safe = {
  walkway:4.0, mainWalkway:5.5, railHeight:1.15, railInset:0.18, doorClearWidth:5.0, doorClearHeight:4.0,
  landingDepth:4.0, jumpGapMax:2.0, jumpRiseMax:0.6, movingClearance:0.35, visualOffset:0.025,
  checkpointFall:2.5, interactionRadius:2.4
};

function esc(v){ return String(v).replace(/[&<>\"]/g,m=>({'&':'&amp;','<':'&lt;','>':'&gt;','\"':'&quot;'}[m])); }
function f(n){ return Number(n).toFixed(1).replace('.0',''); }
function svgDoc(inner,w=1600,h=1000){return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">
<defs><pattern id="grid" width="20" height="20" patternUnits="userSpaceOnUse"><path d="M20 0H0V20" fill="none" stroke="#d9dfe2" stroke-width="1"/></pattern><marker id="arrow" viewBox="0 0 10 10" refX="8" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse"><path d="M0 0L10 5L0 10Z" fill="#d9694f"/></marker><style>
text{font-family:'Malgun Gothic','Noto Sans KR',sans-serif;fill:#273137} .title{font-size:32px;font-weight:700}.sub{font-size:17px;fill:#5f6b70}.label{font-size:18px;font-weight:700}.small{font-size:14px}.tiny{font-size:12px}.dim{font-size:13px;fill:#40545e}.wall{fill:#fbfaf6;stroke:#273137;stroke-width:4}.void{fill:#eef2f2;stroke:#83949b;stroke-width:2;stroke-dasharray:8 6}.route{fill:none;stroke:#d9694f;stroke-width:7;marker-end:url(#arrow)}.zone{fill:#f2c87955;stroke:#b8862f;stroke-width:2;stroke-dasharray:7 5}.camera{fill:#7d6bb055;stroke:#65539b;stroke-width:2}.light{fill:none;stroke:#e0aa38;stroke-width:8;stroke-linecap:round}.datum{stroke:#4b6874;stroke-width:2;stroke-dasharray:5 5}.dimline{stroke:#40545e;stroke-width:1.5;marker-start:url(#arrow);marker-end:url(#arrow)}.panel{fill:#fff;stroke:#aeb9bd;stroke-width:2}.danger{fill:#d9694f22;stroke:#d9694f;stroke-width:2}.paper{fill:#fbfaf6;stroke:#273137;stroke-width:3}.fold{fill:#eef2f2;stroke:#4b6874;stroke-width:2}.prop{fill:#dfe9e3;stroke:#47685b;stroke-width:2}.note{fill:#fff7dc;stroke:#c8a853;stroke-width:2}</style></defs>
<rect width="100%" height="100%" fill="#f6f7f5"/><rect x="40" y="110" width="1040" height="820" fill="url(#grid)" stroke="#aeb9bd" stroke-width="2"/>${inner}</svg>`;}

function footprint(type,cx,cy,w,d){ const x=cx-w/2,y=cy-d/2;
  if(type==='oval') return `<ellipse class="wall" cx="${cx}" cy="${cy}" rx="${w/2}" ry="${d/2}"/>`;
  if(type==='fan') return `<path class="wall" d="M${cx} ${y} Q${x} ${cy} ${x+18} ${y+d} L${cx} ${cy+30} L${x+w-18} ${y+d} Q${x+w} ${cy} ${cx} ${y}Z"/>`;
  if(type==='crescent') return `<path class="wall" fill-rule="evenodd" d="M${x+w*.85} ${y}A${w*.48} ${d*.5} 0 1 0 ${x+w*.85} ${y+d}A${w*.36} ${d*.38} 0 1 1 ${x+w*.85} ${y}Z"/>`;
  if(type==='bowtie') return `<path class="wall" d="M${x} ${y}Q${cx} ${y+d*.18} ${cx} ${cy}Q${cx} ${y+d*.82} ${x} ${y+d}Q${x+w*.3} ${cy} ${x} ${y}ZM${x+w} ${y}Q${cx} ${y+d*.18} ${cx} ${cy}Q${cx} ${y+d*.82} ${x+w} ${y+d}Q${x+w*.7} ${cy} ${x+w} ${y}Z"/>`;
  if(type==='branch') return `<path class="wall" d="M${cx-35} ${y+d}L${cx+35} ${y+d}L${cx+35} ${cy+20}L${x+w} ${cy+15}L${x+w-8} ${y+12}L${cx+18} ${cy-12}L${cx-15} ${cy-8}L${x+10} ${y+8}L${x} ${cy+20}L${cx-35} ${cy+26}Z"/>`;
  if(type==='rings') return `<circle class="wall" cx="${cx}" cy="${cy}" r="${Math.min(w,d)/2}"/><circle fill="#f6f7f5" stroke="#273137" stroke-width="3" cx="${cx}" cy="${cy}" r="${Math.min(w,d)*.25}"/>`;
  if(type==='loom') return `<path class="wall" d="M${x} ${y+12}Q${cx} ${y-10} ${x+w} ${y+12}L${x+w-10} ${y+d}Q${cx} ${y+d-30} ${x+10} ${y+d}Z"/>`;
  if(type==='horseshoe') return `<path class="wall" fill-rule="evenodd" d="M${x} ${y+d}V${cy}A${w/2} ${d/2} 0 0 1 ${x+w} ${cy}V${y+d}H${x+w*.7}V${cy}A${w*.2} ${d*.2} 0 0 0 ${x+w*.3} ${cy}V${y+d}Z"/>`;
  if(type==='pinwheel'||type==='rosette7'){let n=type==='rosette7'?7:4,s='';for(let i=0;i<n;i++){const a=i*Math.PI*2/n,b=a+Math.PI/n;const r1=Math.min(w,d)*.18,r2=Math.min(w,d)*.5;s+=`${i?'L':'M'}${cx+Math.sin(a)*r1} ${cy-Math.cos(a)*r1} Q${cx+Math.sin(b)*r2} ${cy-Math.cos(b)*r2} ${cx+Math.sin(a+Math.PI*2/n)*r1} ${cy-Math.cos(a+Math.PI*2/n)*r1}`;}return `<path class="wall" d="${s}Z"/>`;}
  if(type==='s') return `<path class="wall" d="M${cx+w*.35} ${y}C${x} ${y} ${x} ${cy} ${cx} ${cy}S${x+w} ${y+d} ${cx-w*.35} ${y+d}" stroke-width="${Math.min(w,d)*.32}" fill="none"/><path class="datum" d="M${cx+w*.35} ${y}C${x} ${y} ${x} ${cy} ${cx} ${cy}S${x+w} ${y+d} ${cx-w*.35} ${y+d}" fill="none"/>`;
  if(type==='spiral') return `<path class="wall" d="M${cx} ${cy}m-20 0a20 20 0 1 0 40 0a40 40 0 1 1-80 0a60 60 0 1 0 120 0a80 80 0 1 1-160 0" fill="none" stroke-width="36"/>`;
  if(type==='nest') return `<path class="wall" d="M${x} ${cy+15}Q${x+18} ${y} ${cx} ${y+18}Q${x+w-18} ${y} ${x+w} ${cy+15}Q${x+w-30} ${y+d} ${cx} ${y+d-15}Q${x+30} ${y+d} ${x} ${cy+15}Z"/>`;
  if(type==='trefoil') return `<path class="wall" d="M${cx} ${cy}m0 -${d*.18}A${w*.25} ${d*.25} 0 1 1 ${cx+w*.2} ${cy+d*.14}A${w*.25} ${d*.25} 0 1 1 ${cx-w*.2} ${cy+d*.14}A${w*.25} ${d*.25} 0 1 1 ${cx} ${cy-d*.18}Z"/>`;
  if(type==='bud') return `<path class="wall" d="M${cx} ${y}Q${x+w*.05} ${cy} ${cx} ${y+d}Q${x+w*.95} ${cy} ${cx} ${y}Z"/>`;
  if(type==='figure8') return `<path class="wall" d="M${cx} ${cy}C${x} ${y-10} ${x} ${y+d+10} ${cx} ${cy}C${x+w} ${y-10} ${x+w} ${y+d+10} ${cx} ${cy}Z"/>`;
  return `<rect class="wall" x="${x}" y="${y}" width="${w}" height="${d}" rx="24"/>`;
}

function planRoom(id){
  const r=roomById.get(id), inter=interactionById.get(id), cam=cameraById.get(id), dims=r[2], W=dims[0],D=dims[1],H=dims[2];
  const scale=Math.min(760/W,700/D), cx=550,cy=520, fw=W*scale,fd=D*scale;
  const map=(p)=>[cx+p[0]*scale,cy-p[2]*scale];
  const pts=inter.zones.map(z=>map(z.center));
  const route=`M${pts.map((p,i)=>`${i?'L':''}${p[0]} ${p[1]}`).join(' ')}`;
  const entry=map(cam.entry),exit=map(cam.exit),c1=map(cam.C1.focus);
  let body=`<text class="title" x="40" y="55">${esc(id)} · ${esc(r[1])} — IN-GAME BLOCKOUT BLUEPRINT</text><text class="sub" x="40" y="86">LOCAL ORIGIN (0,0,0) · +Z NORTH · PLAYER 1.2u · REV A / PROPOSED, NOT PLAYTESTED</text>`;
  body+=footprint(silhouettes[id],cx,cy,fw,fd);
  body+=`<line class="datum" x1="${cx}" y1="${cy-fd/2-35}" x2="${cx}" y2="${cy+fd/2+35}"/><line class="datum" x1="${cx-fw/2-35}" y1="${cy}" x2="${cx+fw/2+35}" y2="${cy}"/><text class="label" x="${cx+8}" y="${cy-fd/2-18}">N +Z</text><text class="small" x="${cx+8}" y="${cy+20}">O</text>`;
  body+=`<path class="route" d="${route}"/>`;
  inter.zones.forEach((z,i)=>{const [x,y]=pts[i]; body+=`<circle class="zone" cx="${x}" cy="${y}" r="${Math.min(z.radius*scale,72)}"/><circle class="prop" cx="${x}" cy="${y}" r="8"/><text class="small" x="${x+12}" y="${y-10}">Z${i+1} ${esc(z.name)}</text><text class="tiny" x="${x+12}" y="${y+8}">(${z.center.map(f).join(', ')})</text>`;});
  body+=`<rect class="danger" x="${entry[0]-safe.doorClearWidth*scale/2}" y="${entry[1]-8}" width="${safe.doorClearWidth*scale}" height="16"/><text class="small" x="${entry[0]+12}" y="${entry[1]+28}">ENTRY ${cam.entry.map(f).join(',')}</text><rect class="danger" x="${exit[0]-safe.doorClearWidth*scale/2}" y="${exit[1]-8}" width="${safe.doorClearWidth*scale}" height="16"/><text class="small" x="${exit[0]+12}" y="${exit[1]-14}">EXIT ${cam.exit.map(f).join(',')}</text>`;
  body+=`<path class="camera" d="M${c1[0]} ${c1[1]} l-55 95 h110Z"/><text class="label" x="${c1[0]+12}" y="${c1[1]+24}">C1</text>`;
  body+=`<line class="dimline" x1="${cx-fw/2}" y1="890" x2="${cx+fw/2}" y2="890"/><text class="dim" x="${cx-45}" y="882">X ${W}u</text><line class="dimline" x1="120" y1="${cy-fd/2}" x2="120" y2="${cy+fd/2}"/><text class="dim" x="65" y="${cy}">Z ${D}u</text>`;
  // section
  const sx=1120,base=760,top=200,ys=(base-top)/H;
  body+=`<rect class="panel" x="1100" y="110" width="460" height="820"/><text class="label" x="1125" y="145">SECTION A–A / GAMEPLAY HEIGHT</text><line class="paper" x1="${sx}" y1="${base}" x2="1535" y2="${base}"/><line class="datum" x1="${sx}" y1="${base-H*ys}" x2="1535" y2="${base-H*ys}"/><text class="dim" x="1470" y="${base-H*ys-8}">H ${H}u</text>`;
  const minY=Math.min(...inter.zones.map(z=>z.center[1])),maxY=Math.max(...inter.zones.map(z=>z.center[1]));
  inter.zones.forEach((z,i)=>{const x=sx+30+i*(380/Math.max(1,inter.zones.length-1)), y=base-z.center[1]*ys;body+=`<rect class="fold" x="${x-32}" y="${y-8}" width="64" height="16"/><line class="datum" x1="${x}" y1="${y}" x2="${x}" y2="${base}"/><text class="tiny" x="${x-25}" y="${y-14}">Z${i+1} Y${f(z.center[1])}</text>`;});
  body+=`<line class="dimline" x1="1515" y1="${base}" x2="1515" y2="${base-H*ys}"/><text class="dim" x="1430" y="${base+30}">floor Y ${f(minY)}–${f(maxY)}</text>`;
  if(id==='3B'||id==='3C'||id==='3A') body+=`<line class="light" x1="1140" y1="${base-4*ys}" x2="1510" y2="${base-4*ys}"/><text class="small" x="1150" y="${base-4*ys-12}">P15 BACKLIGHT RESERVED AXIS Y=4u</text>`;
  body+=`<text class="label" x="1125" y="820">IMPLEMENTATION KEYS</text>${mechanics[id].map((m,i)=>`<text class="small" x="1130" y="${850+i*24}">• ${esc(m)}</text>`).join('')}`;
  body+=`<rect class="note" x="650" y="120" width="400" height="110"/><text class="small" x="670" y="150">COMPLETE: ${esc(inter.completionRule)}</text><text class="small" x="670" y="180">RECOVERY: ${esc(inter.recovery)}</text><text class="small" x="670" y="210">CAM: yaw ${cam.C1.rotationEuler[1]}° / pitch 30° / ortho ${cam.C1.orthographicSize}</text>`;
  body+=`<text class="small" x="150" y="920">CLEARANCES: walk ${safe.walkway}u min · main ${safe.mainWalkway}u · door ${safe.doorClearWidth}×${safe.doorClearHeight}u · rail ${safe.railHeight}u · moving ${safe.movingClearance}u · surface offset ${safe.visualOffset}u</text>`;
  return svgDoc(body);
}

function master(){
  const scale=2.35,ox=800,oz=500;
  const pos=id=>[ox+world[id][0]*scale,oz-world[id][2]*scale];
  let body=`<text class="title" x="40" y="55">BOOK TOWER · WORLD BLOCKOUT / STREAMING CELLS</text><text class="sub" x="40" y="86">All dimensions in Unity units · 3A→3B→3C reserved on one +Z backlight axis · room interiors use local origins</text>`;
  for(let i=0;i<order.length-1;i++){if(order[i]==='3C')continue; const a=pos(order[i]),b=pos(order[i+1]);body+=`<path class="route" d="M${a[0]} ${a[1]}L${b[0]} ${b[1]}"/>`;}
  order.forEach(id=>{const r=roomById.get(id),[x,y]=pos(id),w=r[2][0]*scale*.55,d=r[2][1]*scale*.55,wy=world[id][1];body+=`<rect class="wall" x="${x-w/2}" y="${y-d/2}" width="${w}" height="${d}" rx="12"/><text class="label" x="${x-w/2+10}" y="${y-d/2+25}">${id}</text><text class="tiny" x="${x-w/2+10}" y="${y-d/2+44}">Y ${wy} · ${r[2][0]}×${r[2][1]}×${r[2][2]}</text>`;});
  const a=pos('3A'),c=pos('3C');body+=`<line class="light" x1="${c[0]}" y1="${c[1]}" x2="${a[0]}" y2="${a[1]}"/><text class="small" x="${a[0]+15}" y="${(a[1]+c[1])/2}">P15 RESERVED BACKLIGHT AXIS</text>`;
  body+=`<rect class="panel" x="1120" y="120" width="420" height="730"/><text class="label" x="1145" y="160">SCENE / STREAMING PLAN</text><text class="small" x="1145" y="195">Base scene: Player, GameManager, Sun, Audio</text><text class="small" x="1145" y="225">Cells: one additive scene per unique room</text><text class="small" x="1145" y="255">3A-RETURN: state variant of 3A, not new cell</text><text class="small" x="1145" y="285">HD: optional cell, no permanent world bridge</text><text class="small" x="1145" y="325">LOAD</text><text class="small" x="1170" y="350">current + previous + next room</text><text class="small" x="1145" y="385">UNLOAD</text><text class="small" x="1170" y="410">two rooms behind after checkpoint</text><text class="small" x="1145" y="445">BRIDGE SOCKET</text><text class="small" x="1170" y="470">5u clear width / 4u landing overlap</text><text class="small" x="1145" y="505">COORDINATES</text><text class="small" x="1170" y="530">author each cell at local (0,0,0)</text><text class="small" x="1170" y="555">world centers are staging coordinates</text><text class="small" x="1145" y="595">VALIDATION</text><text class="small" x="1170" y="620">no cell bounds overlap at same Y band</text><text class="small" x="1170" y="645">door closed: 5 approach probes blocked</text><text class="small" x="1170" y="670">bridge: forward + reverse traversal</text><text class="small" x="1170" y="695">4 camera yaws: no path occlusion</text><text class="small" x="1170" y="720">moving poses: ≥0.35u clearance</text><text class="small" x="1170" y="745">coplanar surfaces: ≥0.025u offset</text>`;
  body+=`<text class="small" x="70" y="900">FLOW: ${order.join(' → ')} · 3A return occurs after 3C · HD unlocked from either ending</text>`;
  return svgDoc(body);
}

fs.writeFileSync(path.join(out,'00-world-blockout.svg'),master());
for(const id of order) fs.writeFileSync(path.join(out,`${String(index[id]).padStart(2,'0')}-${id}-blueprint.svg`),planRoom(id));
const implementation={revision:'A',status:'proposed_not_playtested',units:'Unity units',playerHeight:1.2,clearances:safe,worldCenters:world,order,roomScenes:Object.fromEntries(order.map(id=>[id,`Room_${id}.unity`])),stateVariants:{'3A':['initial','return_anamorphosis','ending_refold','ending_preserve']},sourceFiles:['rooms.json','interaction-layout.json','camera-layout.json']};
fs.writeFileSync(path.join(out,'implementation-layout.json'),JSON.stringify(implementation,null,2));
console.log(`Generated ${order.length+1} SVG sheets in ${out}`);
