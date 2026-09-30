using System.Linq;
using UnityEngine;
namespace BookTower.Puzzles
{
    // Applied to the existing puzzle: progress, colliders and scene references remain intact.
    public static class ReadingLanternPresentation
    {
        public const string ModelName="MemoryLantern_ArtV2";
        public static void Apply(ReadingRoomPuzzle puzzle)
        {
            if(!puzzle || !puzzle.lantern)return;
            puzzle.recordSocket.localRotation=Quaternion.Euler(0,180,0);
            foreach(var note in puzzle.GetComponentsInChildren<BookTower.UI.ReadableNote>(true))
                if(note.id=="1B.Entry")note.pages=new[]{"빛을 담고 두 걸쇠를 잠가 옮긴다. 같은 홈이 있는 받침에 내려놓고, 두 걸쇠를 열어 빛을 건넨다.","해는 빛을 담는 자리, 책은 기록을 깨우는 자리, 문은 빛을 기다리는 자리. 걸어가는 동안 빛은 닳지 않는다."};
            var model=puzzle.lantern.Find(ModelName);
            if(!model)
            {
                var asset=Resources.Load<GameObject>("ReadingMemoryLantern");
                var tray=Resources.Load<GameObject>("ReadingLanternDock");
                if(!asset||!tray)return;
                foreach(var renderer in puzzle.lantern.GetComponentsInChildren<Renderer>(true))renderer.gameObject.SetActive(false);
                model=Object.Instantiate(asset,puzzle.lantern,false).transform;model.name=ModelName;
                var controls=puzzle.GetComponentsInChildren<ReadingRoomControl>(true);
                foreach(var control in controls)
                {
                    Transform socket=null;
                    switch(control.action)
                    {
                        case ReadingRoomControl.Action.LeftRing: AttachLock(control,model.Find("LeftLatch"));break;
                        case ReadingRoomControl.Action.RightRing: AttachLock(control,model.Find("RightLatch"));break;
                        case ReadingRoomControl.Action.Pickup:
                            HideControlArt(control);
                            control.transform.localPosition=new Vector3(0,2.16f,-.06f);break;
                        case ReadingRoomControl.Action.Charge: socket=puzzle.chargingSocket;break;
                        case ReadingRoomControl.Action.RecordDock: socket=puzzle.recordSocket;break;
                        case ReadingRoomControl.Action.ExitDock: socket=puzzle.exitSocket;break;
                        case ReadingRoomControl.Action.WindowCharge: socket=puzzle.windowChargeSocket;break;
                    }
                    if(!socket)continue;
                    HideControlArt(control);
                    var baseArt=Object.Instantiate(tray,socket,false).transform;baseArt.name="Keyed receiving tray";
                    var plaque=baseArt.Find("sloped illustrated tab");
                    plaque.Find("Book").gameObject.SetActive(control.action==ReadingRoomControl.Action.RecordDock);
                    plaque.Find("Door").gameObject.SetActive(control.action==ReadingRoomControl.Action.ExitDock);
                    plaque.Find("Sun").gameObject.SetActive(control.action==ReadingRoomControl.Action.Charge||control.action==ReadingRoomControl.Action.WindowCharge);
                    control.transform.position=socket.position+Vector3.down*.10f;
                    control.transform.rotation=socket.rotation;
                    var collider=control.GetComponent<BoxCollider>();if(collider){collider.center=Vector3.zero;collider.size=new Vector3(1.96f,.24f,1.96f);}
                }
            }
            puzzle.leftRing=model.Find("LeftLatch");puzzle.rightRing=model.Find("RightLatch");
            puzzle.chargeRibbon=model.Find("ChargeRibbon");
            foreach(var renderer in puzzle.lantern.GetComponentsInChildren<Renderer>(true))
                if(!renderer.transform.IsChildOf(model))renderer.gameObject.SetActive(false);
            // Give the usable plinth a clear approach; these decorative volumes had covered its tab.
            foreach(Transform prop in puzzle.transform)
            {
                if(prop.name=="varied stacked volumes"&&prop.localPosition.x< -7&&prop.localPosition.x> -8&&prop.localPosition.z< -14)
                    prop.localPosition=new Vector3(-10.5f,prop.localPosition.y,prop.localPosition.z);
                if(prop.name=="matching pressed mechanism emblem")prop.gameObject.SetActive(false);
            }
            foreach(var c in puzzle.GetComponentsInChildren<ReadingRoomControl>(true))
                if(c.action==ReadingRoomControl.Action.LeftRing||c.action==ReadingRoomControl.Action.RightRing||c.action==ReadingRoomControl.Action.Pickup||c.action==ReadingRoomControl.Action.Charge||c.action==ReadingRoomControl.Action.RecordDock||c.action==ReadingRoomControl.Action.ExitDock||c.action==ReadingRoomControl.Action.WindowCharge)HideControlArt(c);
            var portrait=Resources.Load<Material>("ReadingRavenPortrait");
            if(portrait)foreach(var t in puzzle.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("06_RavenBanner")))
                foreach(var r in t.GetComponentsInChildren<Renderer>(true))
                {var mats=r.sharedMaterials;bool changed=false;for(int i=0;i<mats.Length;i++)if(mats[i]&&mats[i].name=="Raven_Print"){mats[i]=portrait;changed=true;}if(changed)r.sharedMaterials=mats;}
        }
        static void AttachLock(ReadingRoomControl c,Transform hinge)
        {
            HideControlArt(c);
            c.transform.SetParent(hinge,false);c.transform.localPosition=new Vector3(0,-.2f,-.055f);
            c.transform.localRotation=Quaternion.identity;
            var collider=c.GetComponent<BoxCollider>();if(collider)collider.size=new Vector3(.34f,.42f,.22f);
        }
        static void HideControlArt(ReadingRoomControl c)
        {foreach(var r in c.GetComponentsInChildren<Renderer>(true))if(r.transform!=c.transform)r.gameObject.SetActive(false);}
    }
}
