using UnityEngine;

namespace BookTower.Puzzles
{
    public static class GravityPageRailPhysics
    {
        public const string CargoLayer = "GravityCargo";
        public const float ContactOffset = .0005f;

        public static void Configure(GravityPagePuzzle puzzle)
        {
            int layer = LayerMask.NameToLayer(CargoLayer);
            if (layer < 0)
            {
                Debug.LogError("[GravityPage] Missing GravityCargo physics layer.", puzzle);
                return;
            }
            int cargoMask = 1 << layer;
            foreach (var body in puzzle.cargo)
            {
                foreach (var collider in body.GetComponentsInChildren<Collider>(true))
                {
                    collider.gameObject.layer = layer;
                    collider.contactOffset = ContactOffset;
                }
            }

            // Rounded render meshes generate oblique triangle contacts along a rail.
            // Cargo uses the authored flat shelf box; the player keeps the fitted mesh.
            // Reuse the existing colliders so repeated initialization adds no geometry.
            foreach (var mesh in puzzle.GetComponentsInChildren<MeshCollider>(true))
            {
                if (mesh.name != "고정 선반") continue;
                var rail = mesh.GetComponent<BoxCollider>();
                if (!rail || rail.isTrigger) continue;
                rail.excludeLayers = ~cargoMask;
                rail.layerOverridePriority = 1;
                rail.contactOffset = ContactOffset;
                rail.enabled = true;
                mesh.excludeLayers |= cargoMask;
                mesh.layerOverridePriority = 1;
            }
        }
    }
}
