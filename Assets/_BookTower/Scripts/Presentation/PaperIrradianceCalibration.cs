using UnityEngine;
using UnityEngine.Rendering;

namespace BookTower.Presentation
{
    // The existing real-time art lights use game-scale intensities. Calibrate the baked
    // transport once to that exposure without rebaking/moving any dynamic puzzle light.
    [DefaultExecutionOrder(-150)]
    public sealed class PaperIrradianceCalibration : MonoBehaviour
    {
        public PaperAtmosphere[] rooms;
        public float[] gains;
        LightProbes source;
        SphericalHarmonicsL2[] original;
        public int CalibratedCount { get; private set; }
        void OnEnable() { Apply(); }
        void Start() { if (CalibratedCount == 0) Apply(); }
        void Apply()
        {
            source = LightmapSettings.lightProbes;
            if (!source || source.count == 0 || rooms == null || rooms.Length == 0) return;
            original = source.bakedProbes;
            var calibrated = (SphericalHarmonicsL2[])original.Clone();
            var positions = source.positions;
            for (int i = 0; i < calibrated.Length; i++)
            {
                float nearest = float.PositiveInfinity; int choice = -1;
                for (int r = 0; r < rooms.Length; r++)
                {
                    if (!rooms[r]) continue;
                    float distance = (positions[i] - rooms[r].transform.position).sqrMagnitude;
                    if (distance < nearest) { nearest = distance; choice = r; }
                }
                if (choice < 0) continue;
                float gain = gains != null && choice < gains.Length ? Mathf.Clamp(gains[choice], 0, 1) : .04f;
                for (int c = 0; c < 3; c++) for (int coefficient = 0; coefficient < 9; coefficient++) calibrated[i][c, coefficient] *= gain;
                calibrated[i].AddAmbientLight(new Color(.025f, .036f, .054f));
            }
            source.bakedProbes = calibrated; CalibratedCount = calibrated.Length;
        }
        void OnDisable()
        {
            if (source && original != null && source.count == original.Length) source.bakedProbes = original;
            original = null; source = null; CalibratedCount = 0;
        }
    }
}
