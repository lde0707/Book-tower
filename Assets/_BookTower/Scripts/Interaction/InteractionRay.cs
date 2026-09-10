using UnityEngine;

namespace BookTower.Interaction
{
    /// <summary>
    /// 커서로 물체를 고를 때 쓰는 광선.
    ///
    /// 그냥 Physics.Raycast 를 쓰면 안 되는 이유가 있다.
    ///
    /// 룸 컬링은 지금 방이 아닌 방의 **렌더러만** 끈다. 콜라이더는 살려 둔다 —
    /// 안 그러면 경사로를 오르는 도중에 발밑이 사라진다.
    /// 그런데 카메라가 직교라 광선이 전부 평행하고, 나선형 탑이라
    /// 그 광선이 위층을 통과한다. 실제로 F0 의 조각을 클릭하면
    /// 광선이 2B·2F 의 **보이지도 않는** 바닥에 먼저 맞아서 아무 일도 안 일어났다.
    ///
    /// 화면에 안 보이는 것은 클릭 대상이 될 수 없어야 한다.
    /// 그래서 전부 맞춰 본 뒤, 렌더러가 꺼져 있는 것은 건너뛴다.
    /// </summary>
    public static class InteractionRay
    {
        private static readonly RaycastHit[] Buffer = new RaycastHit[32];

        /// <summary>보이는 것 중 가장 가까운 것을 맞춘다.</summary>
        public static bool Cast(Ray ray, out RaycastHit hit, float maxDistance = 1000f,
                                int layerMask = ~0)
        {
            hit = default;

            int n = Physics.RaycastNonAlloc(ray, Buffer, maxDistance, layerMask);
            if (n <= 0) return false;

            float best = float.MaxValue;
            bool found = false;

            for (int i = 0; i < n; i++)
            {
                if (!IsVisible(Buffer[i].collider)) continue;
                if (Buffer[i].distance >= best) continue;

                best = Buffer[i].distance;
                hit = Buffer[i];
                found = true;
            }
            return found;
        }

        /// <summary>
        /// 이 콜라이더가 지금 화면에 그려지고 있는가.
        ///
        /// 두 가지를 걸러야 한다.
        ///   · 렌더러가 꺼진 것 — 다른 방(룸 컬링)
        ///   · ShadowsOnly 인 것 — 카메라를 향해 숨긴 외벽·천장(면 컬링).
        ///     이쪽은 렌더러가 켜져 있어서 놓치기 쉽다. 눈에는 안 보이는데
        ///     클릭만 가로막으면 플레이어는 조작이 고장 난 줄 안다.
        ///
        /// 렌더러가 아예 없는 물체(순수 충돌체)는 막는 것으로 본다 —
        /// 보이지 않아도 물리적으로는 거기 있기 때문이다.
        /// </summary>
        private static bool IsVisible(Collider col)
        {
            if (col == null) return false;

            var r = col.GetComponent<Renderer>();
            if (r == null) return true;
            if (!r.enabled) return false;
            return r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }
    }
}
