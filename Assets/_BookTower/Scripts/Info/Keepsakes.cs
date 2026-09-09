using System.Collections.Generic;
using UnityEngine;

namespace BookTower.Info
{
    /// <summary>
    /// 유품함 — "정보 = 열쇠" 기믹의 저장소.
    ///
    /// 전역 규칙: **모든 단서는 재열람 가능하다.**
    /// 이게 없으면 정보 지연 노출(1E의 조각을 3A에서 쓰는 설계)이
    /// 퍼즐이 아니라 기억력 테스트로 변질된다.
    /// </summary>
    [DisallowMultipleComponent]
    public class Keepsakes : MonoBehaviour
    {
        public static Keepsakes Instance { get; private set; }

        private readonly List<Keepsake> items = new();

        public IReadOnlyList<Keepsake> Items => items;
        public int Count => items.Count;

        /// <summary>새 단서를 얻었을 때</summary>
        public event System.Action<Keepsake> Collected;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool Has(string id) => items.Exists(k => k.id == id);

        public bool Collect(Keepsake k)
        {
            if (string.IsNullOrEmpty(k.id) || Has(k.id)) return false;

            items.Add(k);
            Collected?.Invoke(k);

            Debug.Log($"[유품함] '{k.label}' 수집 ({items.Count}개){(k.isDecoy ? "  ※가짜" : "")}");
            return true;
        }

        /// <summary>지정한 id들을 전부 가지고 있고, 그 외 가짜는 없는가</summary>
        public bool HasExactly(IEnumerable<string> requiredIds)
        {
            var need = new HashSet<string>(requiredIds);
            foreach (string id in need)
                if (!Has(id)) return false;

            foreach (Keepsake k in items)
                if (k.isDecoy && need.Contains(k.id)) return false;

            return true;
        }

        public static Keepsakes EnsureExists()
        {
            if (Instance != null) return Instance;
            var found = FindFirstObjectByType<Keepsakes>();
            if (found != null) return found;
            return new GameObject("[Keepsakes]").AddComponent<Keepsakes>();
        }
    }
}
