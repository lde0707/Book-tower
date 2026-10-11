using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BookTower.UI
{
    /// <summary>One owner for quality presets; display mode and accessibility stay in settings.</summary>
    internal static class PaperRenderQuality
    {
        public static void Apply(UniversalRenderPipelineAsset pipeline, int quality)
        {
            quality = Mathf.Clamp(quality, 0, 2);
            pipeline.renderScale = quality == 0 ? .75f : quality == 2 ? 1.15f : 1f;
            pipeline.shadowDistance = quality == 0 ? 25 : quality == 2 ? 70 : 45;
            // High retains 4x MSAA. Normal/low use the camera's existing high-quality SMAA.
            // MSAA prevents URP depth priming; removing the duplicate AA pass lets the
            // existing depth prepass reject hidden fragments before expensive lighting.
            pipeline.msaaSampleCount = quality == 2 ? 4 : 1;
        }
    }
}
