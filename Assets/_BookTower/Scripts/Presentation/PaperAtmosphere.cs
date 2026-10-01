using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using BookTower.Rooms;
using BookTower.UI;

namespace BookTower.Presentation
{
    // Artist-authored indirect light and bounded aerial perspective. Puzzle lights are never owned here.
    [DisallowMultipleComponent]
    public sealed class PaperAtmosphere : MonoBehaviour
    {
        public Room room;
        public Vector3 localMinimum, localMaximum;
        [ColorUsage(false, true)] public Color air = new(.24f, .19f, .13f);
        [Range(0, .05f)] public float density = .014f;
        public float clearDistance = 4.5f;
        [ColorUsage(false, true)] public Color indirectBase = new(.10f, .11f, .13f);
        [ColorUsage(false, true)] public Color indirectTop = new(.17f, .15f, .11f);
        [ColorUsage(false, true)] public Color indirectSide = new(.13f, .09f, .05f);
        public Vector3 bounceDirection = new(-1, .5f, 0);
        public Light[] shafts = System.Array.Empty<Light>();
        public Texture2D[] shaftDepth = System.Array.Empty<Texture2D>();
        [Range(0, .2f)] public float shaftDensity = .03f;
        public SphericalHarmonicsL2 Indirect()
        {
            var sh = new SphericalHarmonicsL2();
            sh.AddAmbientLight(indirectBase);
            sh.AddDirectionalLight(Vector3.up, indirectTop, 1);
            sh.AddDirectionalLight(transform.TransformDirection(bounceDirection.normalized), indirectSide, 1);
            sh.AddDirectionalLight(Vector3.down, new Color(.075f, .049f, .027f), 1);
            return sh;
        }
    }

}
