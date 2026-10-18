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
            // Keep UI at display resolution; shade fewer 3D pixels on integrated GPUs.
            // High is native resolution, without the former 15% supersampling cost.
            pipeline.renderScale = quality == 0 ? .67f : quality == 2 ? 1f : .8f;
            pipeline.upscalingFilter = UpscalingFilterSelection.FSR;
            pipeline.fsrOverrideSharpness = true;
            pipeline.fsrSharpness = .35f;
            // No game shader samples _CameraOpaqueTexture; the atmosphere pass
            // obtains its own BlitTexture. Avoid a redundant full-frame color copy.
            pipeline.supportsCameraOpaqueTexture = false;
            pipeline.shadowDistance = quality == 0 ? 25 : quality == 2 ? 70 : 45;
            // High retains 4x MSAA. Normal/low use the camera's existing high-quality SMAA.
            // MSAA prevents URP depth priming; removing the duplicate AA pass lets the
            // existing depth prepass reject hidden fragments before expensive lighting.
            pipeline.msaaSampleCount = quality == 2 ? 4 : 1;
        }
    }
}
