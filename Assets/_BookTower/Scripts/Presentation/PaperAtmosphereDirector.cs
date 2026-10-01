using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using BookTower.Rooms;
using BookTower.UI;

namespace BookTower.Presentation
{
    [DefaultExecutionOrder(150)]
    public sealed class PaperAtmosphereDirector : MonoBehaviour
    {
        public static PaperAtmosphereDirector Instance { get; private set; }
        public bool effectsEnabled = true;
        public bool shaftsEnabled = true;
        public bool indirectEnabled = true;
        PaperAtmosphere[] profiles;
        RoomManager rooms;
        SphericalHarmonicsL2 blended;
        bool initialized;
        readonly Vector4[] minimum = new Vector4[3], maximum = new Vector4[3], air = new Vector4[3], settings = new Vector4[3];
        readonly Vector4[] beamPosition = new Vector4[6], beamDirection = new Vector4[6], beamColor = new Vector4[6], beamRight = new Vector4[6], beamUp = new Vector4[6];
        static readonly int[] depthIds = { Shader.PropertyToID("_PaperBeamDepth0"), Shader.PropertyToID("_PaperBeamDepth1"), Shader.PropertyToID("_PaperBeamDepth2"), Shader.PropertyToID("_PaperBeamDepth3"), Shader.PropertyToID("_PaperBeamDepth4"), Shader.PropertyToID("_PaperBeamDepth5") };
        public int ActiveVolumes { get; private set; }
        public int ActiveShafts { get; private set; }
        public bool OwnsAmbient => isActiveAndEnabled && indirectEnabled && rooms && rooms.Current && rooms.Current.GetComponent<PaperAtmosphere>();
        void OnEnable()
        {
            Instance = this;
            profiles = FindObjectsByType<PaperAtmosphere>(FindObjectsSortMode.None);
            rooms = FindFirstObjectByType<RoomManager>();
        }
        void OnDisable()
        {
            Shader.SetGlobalInt("_PaperAirCount", 0);
            Shader.SetGlobalInt("_PaperBeamCount", 0);
            if (Instance == this) Instance = null;
        }
        void LateUpdate()
        {
            var current = rooms && rooms.Current ? rooms.Current.GetComponent<PaperAtmosphere>() : null;
            if (current && indirectEnabled)
            {
                var target = current.Indirect();
                float k = initialized ? 1 - Mathf.Exp(-2.2f * Time.deltaTime) : 1;
                for (int c = 0; c < 3; c++) for (int i = 0; i < 9; i++) blended[c, i] = Mathf.Lerp(blended[c, i], target[c, i], k);
                initialized = true;
                RenderSettings.ambientMode = AmbientMode.Custom;
                RenderSettings.ambientProbe = blended;
            }
            else initialized = false;

            int n = 0, b = 0;
            foreach (var p in profiles)
            {
                if (!p || !p.enabled || !p.room || !p.room.IsVisible || !effectsEnabled || n >= 3) continue;
                // Scene rooms are axis aligned. No global fog is allowed to seep through another room's wall.
                minimum[n] = p.transform.TransformPoint(p.localMinimum);
                maximum[n] = p.transform.TransformPoint(p.localMaximum);
                air[n] = p.air;
                settings[n] = new Vector4(p.density, p.clearDistance, minimum[n].y, .32f);
                n++;
                if (!shaftsEnabled) continue;
                for (int i = 0; i < p.shafts.Length && b < 6; i++)
                {
                    var l = p.shafts[i];
                    if (!l || !l.enabled || l.intensity <= 0) continue;
                    beamPosition[b] = new Vector4(l.transform.position.x, l.transform.position.y, l.transform.position.z, l.range);
                    var d = l.transform.forward;
                    beamDirection[b] = new Vector4(d.x, d.y, d.z, Mathf.Tan(l.spotAngle * Mathf.Deg2Rad * .5f));
                    beamRight[b] = l.transform.right; beamUp[b] = l.transform.up;
                    Color c = l.color.linear;
                    beamColor[b] = new Vector4(c.r, c.g, c.b, p.shaftDensity * Mathf.Clamp01(l.intensity / 120));
                    Shader.SetGlobalTexture(depthIds[b], i < p.shaftDepth.Length && p.shaftDepth[i] ? p.shaftDepth[i] : Texture2D.whiteTexture);
                    b++;
                }
            }
            ActiveVolumes = n; ActiveShafts = b;
            Shader.SetGlobalInt("_PaperAirCount", n); Shader.SetGlobalInt("_PaperBeamCount", b);
            Shader.SetGlobalVectorArray("_PaperAirMin", minimum); Shader.SetGlobalVectorArray("_PaperAirMax", maximum);
            Shader.SetGlobalVectorArray("_PaperAirColor", air); Shader.SetGlobalVectorArray("_PaperAirSettings", settings);
            Shader.SetGlobalVectorArray("_PaperBeamPosition", beamPosition); Shader.SetGlobalVectorArray("_PaperBeamDirection", beamDirection);
            Shader.SetGlobalVectorArray("_PaperBeamRight", beamRight); Shader.SetGlobalVectorArray("_PaperBeamUp", beamUp); Shader.SetGlobalVectorArray("_PaperBeamColor", beamColor);
            Shader.SetGlobalInt("_PaperAirSteps", PaperSettingsRuntime.Values.quality == 0 ? 16 : PaperSettingsRuntime.Values.quality == 2 ? 40 : 28);
        }
    }
}
