using UnityEngine;
using UnityEngine.UI;
using BookTower.Story;

namespace BookTower.UI
{
    // Persistent illustration layer, independent of the dialogue widgets rebuilt by PaperUI.Draw.
    public sealed class PaperCutsceneView : MonoBehaviour
    {
        PaperCutsceneArt art;
        Image background, previousBackground, spirit, bird, veil, seed, glow;
        Text chapter, nameCard;
        RectTransform scene;
        readonly RectTransform[] motes = new RectTransform[12];
        Sprite glowSprite;
        Texture2D glowTexture;
        StoryShot shot;
        string sequence;
        float age, shotAge, crossfade;
        Vector2 spiritStart, birdStart, spiritTarget, birdTarget;
        Vector3 spiritScaleStart, birdScaleStart, spiritScaleTarget, birdScaleTarget;
        bool hasShot;
        int beatIndex;
        bool illustratedFrame;
        bool firstPerson;
        ContinuousAwakening continuous;
        public ContinuousAwakening Continuous => continuous;
        public bool IsFirstPerson => firstPerson;
        public int FrameIndex => beatIndex;
        public Sprite CurrentFrame => background.sprite;
        public float Elapsed => age;
        public Vector2 SpiritPosition => spirit.rectTransform.anchoredPosition;
        public Vector2 BirdPosition => bird.rectTransform.anchoredPosition;
        public bool IsVisible => gameObject.activeInHierarchy;

        public static PaperCutsceneView Build(Transform parent, PaperCutsceneArt art, Font font)
        {
            var root = Box(parent, "Illustrated story · 2D", 0, 0, 1920, 1080);
            var view = root.gameObject.AddComponent<PaperCutsceneView>();
            view.art = art;
            // Extend an opaque paper matte beyond the safe frame on ultrawide/tall displays.
            Picture(root, "Opaque paper surround", -1920, -1080, 5760, 3240, null, new Color(.27f,.18f,.11f));
            view.scene = Box(root, "Illustrated scene", 960, 540, 1920, 1080);
            view.scene.pivot = new Vector2(.5f,.5f);
            view.background = Picture(view.scene, "Painted background", 0, 0, 1920, 1080, art.foyer, Color.white);
            view.previousBackground = Picture(view.scene, "Page dissolve", 0, 0, 1920, 1080, art.foyer, Color.clear);
            for (int i=0;i<view.motes.Length;i++)
            {
                var mote=Picture(root,"Paper dust "+i,0,0,3+i%3,7+i%4,null,new Color(1,.88f,.58f,.32f));
                view.motes[i]=mote.rectTransform;
            }
            view.spirit=Actor(root,"Illustrated spirit",art.spirit,560);
            view.bird=Actor(root,"Illustrated Moa",art.moa,265);
            view.MakeGlow(root);
            if(art.HasContinuous)view.continuous=ContinuousAwakening.Build(root,art);
            view.chapter=Words(root,font,"",110,70,1300,54,28,new Color(.3f,.17f,.09f));
            view.nameCard=Words(root,font,"모아",850,340,500,120,72,new Color(.32f,.18f,.1f));
            view.nameCard.alignment=TextAnchor.MiddleCenter;
            view.veil=Picture(root,"Opening ink fade",-1920,-1080,5760,3240,null,new Color(.12f,.07f,.04f,0));
            root.gameObject.SetActive(false);
            return view;
        }
        static RectTransform Box(Transform parent,string name,float x,float y,float w,float h)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            r.gameObject.layer=5;r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);
            r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
        }
        static Image Picture(Transform parent,string name,float x,float y,float w,float h,Sprite sprite,Color color)
        {var im=Box(parent,name,x,y,w,h).gameObject.AddComponent<Image>();im.sprite=sprite;im.color=color;im.raycastTarget=false;return im;}
        static Image Actor(Transform parent,string name,Sprite sprite,float size)
        {var im=Picture(parent,name,0,0,size,size,sprite,Color.white);im.preserveAspect=true;im.rectTransform.pivot=new Vector2(.5f,0);return im;}
        static Text Words(Transform parent,Font font,string text,float x,float y,float w,float h,int size,Color color)
        {var t=Box(parent,"Illustration caption",x,y,w,h).gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.color=color;t.text=text;t.raycastTarget=false;return t;}
        void MakeGlow(Transform root)
        {
            glowTexture=new Texture2D(64,64,TextureFormat.RGBA32,false){name="Cutscene soft light",wrapMode=TextureWrapMode.Clamp};
            var colors=new Color[4096];
            for(int y=0;y<64;y++)for(int x=0;x<64;x++)
            {float r=Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f))/31.5f;colors[y*64+x]=new Color(1,.83f,.4f,Mathf.Pow(Mathf.Max(0,1-r),2));}
            glowTexture.SetPixels(colors);glowTexture.Apply();
            glowSprite=Sprite.Create(glowTexture,new Rect(0,0,64,64),new Vector2(.5f,.5f));
            glow=Picture(root,"Borrowed light halo",0,0,120,120,glowSprite,Color.white);glow.rectTransform.pivot=new Vector2(.5f,.5f);
            seed=Picture(root,"Borrowed paper seed",0,0,11,16,null,new Color(1,.87f,.53f));seed.rectTransform.pivot=new Vector2(.5f,.5f);
        }
        public void Begin(string id)
        {
            sequence=id;age=0;hasShot=false;beatIndex=-1;gameObject.SetActive(true);
            firstPerson=id==OpeningStoryData.Promise&&art.HasFirstPerson;
            if(continuous){continuous.gameObject.SetActive(firstPerson);if(firstPerson)continuous.Begin();}
            chapter.enabled=!firstPerson;
            foreach(var mote in motes)mote.gameObject.SetActive(true);
            chapter.text=id==OpeningStoryData.Promise?"첫 장  ·  빌려 온 빛":id==OpeningStoryData.Return?"기억 하나  ·  돌아올 자리":id==OpeningStoryData.Theatre?"접힌 회랑  ·  작은 몸, 큰 그림자":"기억 둘  ·  이름";
        }
        public void ShowShot(StoryShot next)
        {
            beatIndex++;
            bool first=!hasShot;shot=next;shotAge=0;hasShot=true;
            var frame=art.Frame(sequence,beatIndex);
            if(firstPerson)frame=art.firstPersonAwakening[beatIndex<2?0:beatIndex==2?1:3];
            illustratedFrame=frame;
            spirit.enabled=bird.enabled=!illustratedFrame;
            var target=frame?frame:shot==StoryShot.DoorMemory?art.memory:
                sequence==OpeningStoryData.Theatre||sequence==OpeningStoryData.Name?art.theatre:art.foyer;
            if(background.sprite!=target){previousBackground.sprite=background.sprite;previousBackground.color=Color.white;background.sprite=target;crossfade=first?1:0;}
            else crossfade=1;
            bool memory=shot==StoryShot.DoorMemory;
            spiritTarget=new Vector2(memory?360:760,memory?-770:-750);
            birdTarget=new Vector2(memory?1490:1170,memory?-770:-750);
            spiritScaleTarget=Vector3.one*(memory?.52f:1);
            birdScaleTarget=Vector3.one*(memory?.72f:1);
            if(shot==StoryShot.Knock)spiritScaleTarget=new Vector3(1.08f,.72f,1);
            if(shot==StoryShot.NameMemory){spiritTarget.x=590;birdTarget=new Vector2(1110,-735);birdScaleTarget=Vector3.one*1.15f;}
            if(first){spirit.rectTransform.anchoredPosition=spiritTarget;bird.rectTransform.anchoredPosition=birdTarget;spirit.transform.localScale=spiritScaleTarget;bird.transform.localScale=birdScaleTarget;}
            spiritStart=spirit.rectTransform.anchoredPosition;birdStart=bird.rectTransform.anchoredPosition;
            spiritScaleStart=spirit.transform.localScale;birdScaleStart=bird.transform.localScale;
            Refresh(0);
        }
        public void Tick(float dt){age+=dt;shotAge+=dt;crossfade=Mathf.Min(1,crossfade+dt*2);Refresh(dt);}
        void Refresh(float dt)
        {
            bool reduced=PaperSettingsRuntime.Values.reduceStoryMotion;
            float motion=reduced?0:1,t=Mathf.SmoothStep(0,1,Mathf.Clamp01(shotAge/1.1f));
            Vector2 s=Vector2.Lerp(spiritStart,spiritTarget,t),b=Vector2.Lerp(birdStart,birdTarget,t);
            s.y+=Mathf.Sin(age*1.7f)*7*motion;
            b.y+=Mathf.Sin(age*2.3f)*3*motion;
            float lean=Mathf.Sin(age*1.5f)*1.4f*motion,birdLean=Mathf.Sin(age*2)*2*motion;
            if(shot==StoryShot.Knock){b.x-=Mathf.Abs(Mathf.Sin(Mathf.Min(shotAge,2.1f)*Mathf.PI*3/2.1f))*30*motion;lean-=8*motion;}
            if(shot==StoryShot.FailedFlight){float lift=Mathf.Sin(Mathf.Clamp01(shotAge/3.2f)*Mathf.PI);b.y+=lift*110*motion;birdLean+=lift*7*motion;}
            spirit.rectTransform.anchoredPosition=s;bird.rectTransform.anchoredPosition=b;
            spirit.transform.localScale=Vector3.Lerp(spiritScaleStart,spiritScaleTarget,t)*(1+Mathf.Sin(age*1.8f)*.006f*motion);
            bird.transform.localScale=Vector3.Lerp(birdScaleStart,birdScaleTarget,t);
            spirit.transform.localRotation=Quaternion.Euler(0,0,lean);bird.transform.localRotation=Quaternion.Euler(0,0,birdLean);
            float drift=Mathf.SmoothStep(0,1,Mathf.Clamp01(shotAge/12));
            scene.localScale=Vector3.one*(1.02f+drift*.018f*motion);
            scene.anchoredPosition=new Vector2(960+(beatIndex%2==0?1:-1)*drift*9*motion,-540+drift*4*motion);
            previousBackground.color=new Color(1,1,1,1-crossfade);
            veil.color=new Color(.12f,.07f,.04f,Mathf.Max(0,1-age/1.15f));
            for(int i=0;i<motes.Length;i++)
            {float phase=age*.15f+i*1.7f;var m=motes[i];m.anchoredPosition=new Vector2(180+(i*139)%1550+Mathf.Sin(phase)*12*motion,-200-(i*83)%540+Mathf.Cos(phase*.7f)*16*motion);m.localRotation=Quaternion.Euler(0,0,i*29+age*6*motion);}
            bool transfer=shot==StoryShot.ShareLight&&shotAge<3.2f;
            bool receiving=illustratedFrame&&sequence==OpeningStoryData.Promise&&beatIndex==3&&shotAge<4;
            seed.gameObject.SetActive(transfer);glow.gameObject.SetActive(transfer);
            if(transfer)
            {
                float lightT=Mathf.SmoothStep(0,1,Mathf.Clamp01(shotAge/3));
                Vector2 p=illustratedFrame?Vector2.Lerp(new Vector2(1190,-510),new Vector2(820,-560),lightT)+Vector2.up*Mathf.Sin(lightT*Mathf.PI)*85:
                    Vector2.Lerp(b+new Vector2(-65,130),s+new Vector2(20,320),lightT)+Vector2.up*Mathf.Sin(lightT*Mathf.PI)*65;
                seed.rectTransform.anchoredPosition=glow.rectTransform.anchoredPosition=p;
                seed.transform.localRotation=Quaternion.Euler(0,0,25+shotAge*30*motion);
                glow.color=new Color(1,1,1,Mathf.Sin(Mathf.Clamp01(shotAge/3.2f)*Mathf.PI)*.8f);
                glow.rectTransform.sizeDelta=Vector2.one*150;
            }
            if(receiving)
            {
                glow.gameObject.SetActive(true);
                glow.rectTransform.anchoredPosition=new Vector2(830,-560);
                glow.rectTransform.sizeDelta=Vector2.one*(reduced?260:Mathf.Lerp(100,440,Mathf.Clamp01(shotAge/4)));
                glow.color=new Color(1,.91f,.65f,Mathf.Sin(Mathf.Clamp01(shotAge/4)*Mathf.PI)*.35f);
            }
            nameCard.gameObject.SetActive(shot==StoryShot.NameMemory);
            nameCard.color=new Color(.32f,.18f,.1f,Mathf.Clamp01(shotAge/.9f));
            if(firstPerson&&continuous)
            {
                spirit.enabled=bird.enabled=false;chapter.enabled=false;nameCard.gameObject.SetActive(false);
                seed.gameObject.SetActive(false);glow.gameObject.SetActive(false);veil.color=Color.clear;
                continuous.Render(age,beatIndex,dt,reduced);
            }
            else if(firstPerson)RefreshFirstPerson(reduced,motion);
        }
        void RefreshFirstPerson(bool reduced,float motion)
        {
            spirit.enabled=bird.enabled=false;
            chapter.enabled=false;nameCard.gameObject.SetActive(false);
            seed.gameObject.SetActive(false);
            bool dark=beatIndex<2,offering=beatIndex==2;
            float u=Mathf.SmoothStep(0,1,Mathf.Clamp01(shotAge/4));
            // The first image stays mostly hidden: consciousness, rather than a room reveal.
            float darkness=dark?(beatIndex==0?Mathf.Lerp(1,.86f,Mathf.Clamp01(shotAge/3)):Mathf.Lerp(.86f,.60f,u)):
                offering?Mathf.Lerp(.48f,0,u):0;
            veil.color=new Color(.025f,.017f,.012f,darkness);
            for(int i=0;i<motes.Length;i++)motes[i].gameObject.SetActive(!dark);
            if(offering&&shotAge>=1.8f&&background.sprite!=art.firstPersonAwakening[2])
            {
                previousBackground.sprite=background.sprite;
                background.sprite=art.firstPersonAwakening[2];crossfade=0;
            }
            scene.localScale=Vector3.one*(1.025f+(dark?u*.025f:Mathf.Sin(age*.8f)*.003f)*motion);
            scene.anchoredPosition=new Vector2(960+Mathf.Sin(age*.65f)*3*motion,-540+Mathf.Sin(age*.9f)*3*motion);
            bool receive=beatIndex==3&&shotAge<3;
            glow.gameObject.SetActive(offering||receive);
            if(offering)
            {
                // Expanding toward the eye and descending into the viewer's chest.
                float approach=Mathf.SmoothStep(0,1,Mathf.Clamp01((shotAge-.7f)/3));
                glow.rectTransform.anchoredPosition=new Vector2(960,-Mathf.Lerp(575,810,approach));
                glow.rectTransform.sizeDelta=Vector2.one*(reduced?650:Mathf.Lerp(120,2100,approach));
                glow.color=new Color(1,.86f,.55f,Mathf.Sin(approach*Mathf.PI)*.42f);
            }
            if(receive)
            {
                glow.rectTransform.anchoredPosition=new Vector2(960,-820);
                glow.rectTransform.sizeDelta=Vector2.one*1900;
                glow.color=new Color(1,.88f,.62f,.25f*(1-Mathf.Clamp01(shotAge/3)));
            }
        }
        public void End(){gameObject.SetActive(false);hasShot=false;}
        void OnDestroy(){if(glowSprite)Destroy(glowSprite);if(glowTexture)Destroy(glowTexture);}
    }
}
