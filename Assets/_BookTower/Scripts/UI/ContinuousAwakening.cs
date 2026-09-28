using UnityEngine;
using UnityEngine.UI;
using BookTower.Story;

namespace BookTower.UI
{
    // A single stage: dialogue never replaces the bird or resets the gift timeline.
    public sealed class ContinuousAwakening : MonoBehaviour
    {
        Image body,offering,hands,seed,halo,darkness;
        Texture2D lightTexture;Sprite lightSprite;
        float giftTime;
        public float GiftTime=>giftTime;
        public bool LightArrived=>giftTime>=3.8f;
        public const float BirdFullyVisibleAt=4.45f;
        public const float BirdHoldSeconds=3f;
        public bool AwakeningReady=>giftTime>=BirdFullyVisibleAt+BirdHoldSeconds;
        public bool HandsVisible=>hands.enabled;
        public float BirdVisibility=>body.color.a+offering.color.a;
        public Vector2 BirdAnchor=>body.rectTransform.anchoredPosition;
        static Image Picture(Transform parent,string name,Sprite sprite,Vector2 size,Vector2 pivot)
        {
            var image=new GameObject(name,typeof(RectTransform),typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent,false);image.gameObject.layer=5;
            var r=image.rectTransform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=pivot;r.sizeDelta=size;
            image.sprite=sprite;image.raycastTarget=false;return image;
        }
        public static ContinuousAwakening Build(Transform parent,PaperCutsceneArt art)
        {
            var root=new GameObject("Continuous first-person stage",typeof(RectTransform));root.layer=5;root.transform.SetParent(parent,false);
            var rect=(RectTransform)root.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.sizeDelta=new Vector2(1920,1080);
            var v=root.AddComponent<ContinuousAwakening>();
            var bg=Picture(root.transform,"Quiet dark paper",null,new Vector2(5760,3240),new Vector2(.5f,.5f));bg.rectTransform.anchoredPosition=new Vector2(960,-540);bg.color=new Color(.055f,.035f,.023f);
            v.body=Picture(root.transform,"Bird fixed on right",art.continuousBird,new Vector2(790,790),new Vector2(.5f,.5f));
            v.offering=Picture(root.transform,"Whole bird offering pose",art.continuousOffering,new Vector2(790,790),new Vector2(.5f,.5f));
            v.hands=Picture(root.transform,"My paper hands",art.continuousHands,new Vector2(1480,1480),new Vector2(.5f,.5f));
            v.seed=Picture(root.transform,"Shared paper light",art.continuousSeed,new Vector2(90,90),new Vector2(.5f,.5f));
            v.lightTexture=new Texture2D(64,64,TextureFormat.RGBA32,false);var pixels=new Color[4096];
            for(int y=0;y<64;y++)for(int x=0;x<64;x++){float d=Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f))/31.5f;pixels[y*64+x]=new Color(1,.76f,.35f,Mathf.Pow(Mathf.Max(0,1-d),2));}
            v.lightTexture.SetPixels(pixels);v.lightTexture.Apply();v.lightSprite=Sprite.Create(v.lightTexture,new Rect(0,0,64,64),Vector2.one*.5f);
            v.halo=Picture(root.transform,"Shared warm halo",v.lightSprite,Vector2.one*500,Vector2.one*.5f);
            v.darkness=Picture(root.transform,"Consciousness",null,new Vector2(5760,3240),Vector2.one*.5f);v.darkness.rectTransform.anchoredPosition=new Vector2(960,-540);
            root.SetActive(false);return v;
        }
        public void Begin(){giftTime=0;gameObject.SetActive(true);}
        static float Ease(float v)=>Mathf.SmoothStep(0,1,Mathf.Clamp01(v));
        public void Render(float age,int beat,float dt,bool reduced)
        {
            if(beat>=2)giftTime+=dt;
            float enter=reduced?1:Ease(age/2.5f),reach=Ease(giftTime/1.35f),travel=Ease((giftTime-1.1f)/2.6f),absorb=Ease((giftTime-4.0f)/1.4f);
            reach*=1-.85f*Ease((giftTime-6)/2);
            float bob=reduced?0:Mathf.Sin(age*1.3f)*3;
            // Body and shoulder share the same offset throughout every line.
            var offset=new Vector2((1-enter)*320,bob);
            body.rectTransform.anchoredPosition=new Vector2(1320,-400)+offset;
            offering.rectTransform.anchoredPosition=body.rectTransform.anchoredPosition;
            float reveal=Ease((giftTime-3.8f)/.65f);
            body.color=new Color(1,1,1,(1-reach)*reveal);offering.color=new Color(1,1,1,reach*reveal);
            hands.rectTransform.anchoredPosition=new Vector2(900,Mathf.Lerp(-1180,-570,Ease((giftTime-.6f)/2.5f))-absorb*30);
            hands.enabled=false;
            Vector2 origin=new Vector2(1120,-450);
            Vector2 target=new Vector2(960,-490);
            Vector2 p=Vector2.Lerp(origin,target,travel)+Vector2.up*(Mathf.Sin(travel*Mathf.PI)*75);
            seed.rectTransform.anchoredPosition=p;
            seed.rectTransform.sizeDelta=Vector2.one*Mathf.Lerp(25,reduced?290:620,travel);
            seed.transform.localRotation=Quaternion.Euler(0,0,Mathf.Lerp(-18,12,travel));
            float visible=Ease((giftTime-.55f)/.5f)*(1-absorb);
            seed.color=new Color(1,1,1,visible);
            halo.rectTransform.anchoredPosition=p;
            halo.rectTransform.sizeDelta=Vector2.one*(reduced?1100:Mathf.Lerp(180,3000,travel));
            halo.color=new Color(1,1,1,visible*.45f+absorb*.13f);
            float dim=beat<2?Mathf.Lerp(1,.83f,Ease(age/3)):Mathf.Lerp(.83f,.04f,Ease(giftTime/4.5f));
            darkness.color=new Color(.01f,.007f,.005f,dim);
        }
        void OnDestroy(){if(lightSprite)DestroyImmediate(lightSprite);if(lightTexture)DestroyImmediate(lightTexture);}
    }
}
