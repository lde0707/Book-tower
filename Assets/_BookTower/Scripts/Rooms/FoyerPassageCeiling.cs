using UnityEngine;
using UnityEngine.SceneManagement;
namespace BookTower.Rooms
{
    public static class FoyerPassageCeiling
    {
        public const string RootName="F0_1A_연속종이천장";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register(){SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;}
        static void Loaded(Scene scene,LoadSceneMode mode)
        {
            if(scene.name!="InGame")return;
            foreach(var root in scene.GetRootGameObjects())if(root.name==RootName)return;
            var asset=Resources.Load<GameObject>("FoyerPassageCeiling");
            if(asset){var instance=Object.Instantiate(asset);instance.name=RootName;SceneManager.MoveGameObjectToScene(instance,scene);}
        }
    }
}
