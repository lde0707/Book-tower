using UnityEngine;
using UnityEngine.UI;

namespace BookTower.UI
{
    // Motion stays in the illustrated world; title lettering and menu hit areas stay still.
    public sealed class PaperTitleMotion : MonoBehaviour
    {
        static readonly int TimeId = Shader.PropertyToID("_TitleTime");
        static readonly int MotionId = Shader.PropertyToID("_Motion");
        static readonly int LightId = Shader.PropertyToID("_Light");
        Material ownedMaterial;
        float age;
        public float Age => age;
        public bool MotionEnabled => !PaperSettingsRuntime.Values.reduceStoryMotion;
        public void Initialize(RawImage image, Shader shader)
        {
            if (!shader) return;
            ownedMaterial = new Material(shader) { name = "Title paper motion (runtime)" };
            image.material = ownedMaterial;
        }
        void Update()
        {
            if (!ownedMaterial) return;
            if (MotionEnabled) age += Time.unscaledDeltaTime;
            ownedMaterial.SetFloat(TimeId, age);
            ownedMaterial.SetFloat(MotionId, MotionEnabled ? 1 : 0);
            ownedMaterial.SetFloat(LightId, MotionEnabled && !PaperSettingsRuntime.Values.reduceFlash ? 1 : 0);
        }
        void OnDestroy() { if (ownedMaterial) Destroy(ownedMaterial); }
    }
}
