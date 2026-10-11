using UnityEngine;

namespace BookTower.Rooms
{
    /// <summary>
    /// Room culling owns visibility only, never object activation or collision.
    /// Restore the states captured on hide so authored disabled lights stay disabled.
    /// </summary>
    internal sealed class RoomVisibility
    {
        readonly Renderer[] renderers;
        readonly Light[] lights;
        readonly bool[] rendererStates, lightStates;
        public bool IsVisible { get; private set; } = true;

        public RoomVisibility(Component owner)
        {
            renderers = owner.GetComponentsInChildren<Renderer>(true);
            lights = owner.GetComponentsInChildren<Light>(true);
            rendererStates = new bool[renderers.Length];
            lightStates = new bool[lights.Length];
        }

        public void SetVisible(bool visible)
        {
            if (visible == IsVisible) return;
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (!renderer) continue;
                if (!visible) rendererStates[i] = renderer.enabled;
                renderer.enabled = visible && rendererStates[i];
            }
            for (int i = 0; i < lights.Length; i++)
            {
                var light = lights[i];
                if (!light) continue;
                if (!visible) lightStates[i] = light.enabled;
                light.enabled = visible && lightStates[i];
            }
            IsVisible = visible;
        }
    }
}
