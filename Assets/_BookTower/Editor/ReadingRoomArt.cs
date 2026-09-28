using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace BookTower.EditorTools {
 public static partial class ReadingRoomBuilder {
  static Material bannerPrint,windowPrint,portraitPrint;
  static Material Atlas(string name,Vector4 rect,Color tint,float emission=0,bool repeat=true,string texture="ReadingAtlas.png") {
   string path=Dir+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!m){m=new Material(Shader.Find("BookTower/ReadingPaper"));AssetDatabase.CreateAsset(m,path);}m.shader=Shader.Find("BookTower/ReadingPaper");
   m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_BookTower/Textures/ReadingRoom1B/"+texture));m.SetVector("_Rect",rect);m.SetColor("_BaseColor",tint);m.SetFloat("_Emission",emission);m.SetFloat("_Repeat",repeat?1:0);EditorUtility.SetDirty(m);return m;
  }
  static void ArtMaterials() {
   paper=Atlas("Ivory_Atlas",new Vector4(.018f,.018f,.464f,.464f),new Color(1,.96f,.88f));
   navy=pass>=4?Mat("Indigo_QuietFiber",new Color(.24f,.29f,.40f)):Atlas("Indigo_Atlas",new Vector4(.518f,.518f,.464f,.464f),new Color(.78f,.85f,1));
   bannerPrint=Atlas("Raven_Print",new Vector4(.018f,.518f,.464f,.464f),Color.white,0,false);
   windowPrint=Atlas("Owl_Window",new Vector4(.518f,.018f,.464f,.464f),Color.white,.8f,false);
   portraitPrint=Atlas("Bookbinder_Print",new Vector4(.003f,.003f,.994f,.994f),Color.white,0,false,"Bookbinder.png");
   covers[0]=navy;if(pass>=4){gold=Mat("Worn_Brass",new Color(.8f,.6f,.29f),.25f);covers[1]=Mat("Warm_Wine_Cover",new Color(.44f,.16f,.20f));covers[2]=Mat("Faded_Ochre_Cover",new Color(.56f,.4f,.21f));covers[3]=Mat("Dusty_Mauve_Cover",new Color(.35f,.25f,.37f));}
  }
  static Transform Print(Transform p,string name,Vector3 at,float w,float h,Material mat,Vector3 rotation,bool torn=false) {
   var mesh=F0Mesh.Paper(w,h,seed++);var v=mesh.vertices;var uv=mesh.uv;
   for(int i=0;i<v.Length;i++){uv[i]=new Vector2((v[i].x+w*.5f)/w,v[i].y/h);if(torn&&v[i].y<.1f)v[i].y+=.11f*Mathf.Sin(v[i].x*17)+.055f*Mathf.Cos(v[i].x*29);}
   mesh.vertices=v;mesh.uv=uv;mesh.RecalculateNormals();mesh.RecalculateBounds();var t=MeshAt(p,name,SaveMesh(mesh),mat,at);t.localEulerAngles=rotation;return t;
  }
  static Transform Chair(Transform p,Vector3 at,float yaw) {
   var g=Group(p,"08_GothicReadingChair",at);g.localEulerAngles=V(0,yaw,0);
   foreach(float x in new[]{-1.8f,1.8f})foreach(float z in new[]{-1.5f,1.5f}) {
    Tube(g,"curved laminated leg",new[]{V(x*1.2f,0,z*1.1f),V(x,1,z),V(x*.9f,2.8f,z*.85f)},.20f,navy);
    Box(g,"brass foot",V(x*1.2f,.15f,z*1.1f),V(.48f,.3f,.48f),gold);
   }
   Box(g,"stitched burgundy cushion",V(0,2.9f,0),V(4,.55f,3.7f),wine,true);
   foreach(float x in new[]{-1.85f,1.85f}) {
    Tube(g,"scrolling armrest",new[]{V(x,3.1f,-1.4f),V(x,4.4f,-1.2f),V(x,4.7f,-.5f),V(x,4.6f,1.5f)},.19f,navy);
    Box(g,"tall back stile",V(x,6,1.55f),V(.28f,7.5f,.32f),navy);
   }
   Print(g,"raven upholstered back",V(0,3.2f,1.40f),3.2f,3.2f,bannerPrint,Vector3.zero,true);
   for(int j=-1;j<=1;j++) {
    var arch=new List<Vector3>();for(int i=0;i<=24;i++){float a=i*Mathf.PI/24;arch.Add(V(j*1.1f+Mathf.Cos(a)*.53f,7.1f+Mathf.Sin(a)*1.75f,1.5f));}
    Tube(g,"pointed tracery",arch,.09f,gold);foreach(float s in new[]{-.53f,.53f})Tube(g,"tracery upright",new[]{V(j*1.1f+s,5.5f,1.5f),V(j*1.1f+s,7.1f,1.5f)},.075f,gold);
   }
   Tube(g,"crown silhouette",new[]{V(-1.85f,9.6f,1.55f),V(-1.3f,9.7f,1.55f),V(0,11,1.55f),V(1.3f,9.7f,1.55f),V(1.85f,9.6f,1.55f)},.16f,navy);
   Ring(g,"crown quatrefoil",V(0,9.35f,1.45f),.42f,.09f,gold);Merge(g);return g;
  }
  static void ArchitecturalArt() {
   var chair=Chair(root,V(-14,0,-14),-55);modules.Add(chair);
   foreach(float side in new[]{-1f,1f})for(int z=-20;z<=20;z+=7) {
    var g=Group(root,"layered library pilaster",V(side*17.1f,0,z));
    Box(g,"indigo binding column",V(0,8,0),V(.52f,16,1),navy,true);
    foreach(float y in new[]{.3f,6,12,16})Box(g,"gilt capital",V(0,y,0),V(.75f,.18f,1.25f),gold);
   }
   foreach(float side in new[]{-1f,1f})for(int z=-17;z<=18;z+=7) {
    var a=Group(root,"upper archival cartouche",V(side*17.35f,12.8f,z));a.localEulerAngles=V(0,side<0?-90:90,0);
    Print(a,"pressed raven crest",Vector3.zero,3.4f,3,bannerPrint,Vector3.zero,true);
   }
   var portrait=Group(root,"10_BookbinderPortrait",V(7.8f,8.8f,21.05f));
   Print(portrait,"bookbinder and companion",Vector3.zero,7.4f,4.2f,portraitPrint,Vector3.zero);
   foreach(float x in new[]{-3.85f,3.85f})Box(portrait,"frame stile",V(x,2.1f,-.1f),V(.2f,4.6f,.3f),gold);
   foreach(float y in new[]{-.1f,4.3f})Box(portrait,"frame rail",V(0,y,-.1f),V(7.9f,.2f,.3f),gold);modules.Add(portrait);
   foreach(float z in new[]{-19f,-10,8,17}) {
    var l=Lantern(root,V(0,15.4f,z));l.localScale=Vector3.one*1.4f;Rope(root,V(0,18.8f,z),V(0,18.15f,z),.04f);
    Point(root,V(0,14.5f,z),30,23,new Color(1,.75f,.45f));
   }
   if(pass>=4)FinalArt();
  }
  static void FinalArt() {
   // Fill the entrance wall with a different rhythm rather than another identical wall of shelves.
   foreach(float x in new[]{-3f,3,9,15})Shelf(root,V(x,0,-20.8f),180);
   var crest=Group(root,"entrance illuminated manuscript",V(3,13,-21.5f));crest.localEulerAngles=V(0,180,0);
   Print(crest,"entry bookbinder mural",Vector3.zero,12,5.6f,portraitPrint,Vector3.zero);
   foreach(float x in new[]{-6.2f,6.2f})Box(crest,"manuscript edge binding",V(x,2.8f,-.08f),V(.15f,5.8f,.22f),gold);
   foreach(float x in new[]{-12f,12f})Banner(root,V(x,9.5f,21.45f),0);
   foreach(float x in new[]{-3.15f,3.15f}) {
    Box(root,"door book-spine jamb",V(x,3.2f,20.97f),V(.38f,6.5f,.6f),navy);
    for(int j=0;j<6;j++)Box(root,"door jamb raised binding",V(x,.4f+j*1.1f,20.62f),V(.5f,.09f,.15f),gold);
   }
   Box(root,"carved door lintel",V(0,6.35f,20.93f),V(6.7f,.28f,.65f),gold);
   var gothic=new List<Vector3>();for(int side=-1;side<=1;side+=2)for(int i=0;i<=24;i++){float t=side<0?i/24f:1-i/24f;gothic.Add(V(side*(2.7f*(1-t*t)),3.2f+3.7f*t,20.5f));}
   Tube(root,"pointed gothic door surround",gothic,.15f,gold);
   foreach(float side in new[]{-1f,1f}){Tube(root,"gothic door upright",new[]{V(side*2.7f,.1f,20.5f),V(side*2.7f,3.2f,20.5f)},.14f,gold);var l=Lantern(root,V(side*3.95f,3.3f,20.25f));l.localScale=Vector3.one*.65f;Point(root,V(side*3.95f,3.8f,19.7f),10,9,new Color(1,.64f,.31f));}
   // A visible linkage makes window-before-door a causal relationship.
   Tube(root,"visible latch linkage",new[]{V(3.7f,2.3f,20.7f),V(3.7f,6.8f,20.7f),V(-3.7f,6.8f,20.7f),V(-3.7f,1,18.5f)},.04f,gold);
   for(int i=0;i<3;i++) {
    var niche=Group(root,"recessed wall manuscript",V(-13+i*12,14.5f,21.4f));
    Print(niche,"illuminated border",Vector3.zero,2.2f,3.2f,bannerPrint,Vector3.zero,true);
   }
   // Freestanding reading props: open pages, feather quills and little ink vessels.
   foreach(var at in new[]{V(-10.4f,5.75f,-8),V(10,3.92f,10)}) {
    var g=Group(root,"writing still life",at);
    Print(g,"open illustrated page",V(-.65f,.08f,0),1.2f,1.6f,portraitPrint,V(70,0,-8));
    Print(g,"turned illustrated page",V(.65f,.08f,0),1.2f,1.6f,bannerPrint,V(70,0,8));
    Box(g,"ink bottle",V(1.9f,.2f,.2f),V(.35f,.4f,.35f),ink);Leaf(g,V(1.9f,.4f,.2f),1.1f,-30,paper);
   }
   foreach(var at in new[]{V(-14,1.6f,-8.8f),V(-3.7f,1.1f,18.5f),V(5.7f,1.1f,18.5f)}) {
    var g=Group(root,"matching pressed mechanism emblem",at);Ring(g,"embossed lamp socket",Vector3.zero,.4f,.035f,gold);Sprig(g,V(0,-.23f,-.02f),.45f,gold);
   }
   puzzle.exitFlower=Group(root,"exit responsive paper flower",V(-3.7f,1.6f,18.45f));for(int i=0;i<6;i++)Leaf(puzzle.exitFlower,Vector3.zero,.34f,i*60,gold);
   var hazePath=Dir+"/Materials/Window_Haze.mat";var haze=AssetDatabase.LoadAssetAtPath<Material>(hazePath);if(!haze){haze=new Material(Shader.Find("BookTower/WindowHaze"));AssetDatabase.CreateAsset(haze,hazePath);}haze.SetColor("_Color",new Color(.62f,.75f,1,.10f));EditorUtility.SetDirty(haze);
   for(int i=0;i<3;i++) {
    Vector3 a=V(-2+i*2,14,20.7f),b=V(-8+i*2,1,7),across=V(.7f,0,0);var m=new Mesh();m.vertices=new[]{a-across,a+across,b-across*2,b+across*2};m.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(0,1),new Vector2(1,1)};m.triangles=new[]{0,1,2,2,1,3};m.RecalculateNormals();m.RecalculateBounds();var beam=MeshAt(root,"soft blue window scattering",SaveMesh(m),haze,Vector3.zero);beam.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
   }
   KeyLight(V(-7,13,-6),V(-8,0,-9),new Color(1,.72f,.42f),75);
   KeyLight(V(1,14,19),V(-2,0,2),new Color(.48f,.64f,1),85);
  }
  static void KeyLight(Vector3 at,Vector3 target,Color color,float intensity) {
   var g=Group(root,"shadow-casting reading light",at);g.LookAt(root.TransformPoint(target));var l=g.gameObject.AddComponent<Light>();l.type=LightType.Spot;l.color=color;l.intensity=intensity;l.range=35;l.spotAngle=75;l.innerSpotAngle=50;l.shadows=LightShadows.Soft;l.shadowBias=.05f;l.shadowNormalBias=.18f;
  }
 }
}
