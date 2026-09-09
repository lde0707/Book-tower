using System.Collections;
using UnityEngine;

namespace BookTower.EchoSystem
{
    /// <summary>
    /// 주름 — "에코" 기믹의 단위.
    ///
    /// 종이의 주름은 그 페이지가 과거에 어떻게 접히고 읽혔는지의 기록이다.
    /// 문지르면 그때의 행동이 반투명한 잔상으로 재생된다.
    ///
    /// ⚠ 설계 원칙: 에코는 **정답 자판기가 아니다.**
    /// 재생은 결과만 보여주고 방법은 가려야 한다(P8의 가림막처럼).
    /// 그래서 이 컴포넌트는 "무엇을 보여줄지"를 경로로만 정의하고,
    /// 가리는 연출은 씬 배치(가림막 오브젝트)가 담당한다.
    /// </summary>
    [DisallowMultipleComponent]
    public class EchoCrease : MonoBehaviour
    {
        [Header("식별")]
        [SerializeField] private string creaseId = "";

        [Tooltip("깨울 수 있는 주름인가. 활성 주름만 빛을 머금는 시각 규칙을 지킬 것")]
        [SerializeField] private bool active = true;

        [Header("마모 — P11 순서 판정의 근거 ①")]
        [Tooltip("0=가장 오래됨, 1=가장 최근. 형태(결의 닳음)로도 표현해야 한다 — 명도만으로 구분 금지")]
        [Range(0f, 1f)][SerializeField] private float wear = 0.5f;

        [Header("재생 경로")]
        [Tooltip("잔상이 지나갈 지점들. 비어 있으면 제자리에서 잠깐 나타났다 사라진다")]
        [SerializeField] private Transform[] path;
        [SerializeField] private float playSeconds = 3.5f;

        [Header("상호작용")]
        [SerializeField] private float interactRange = 3f;

        public string CreaseId => string.IsNullOrEmpty(creaseId) ? name : creaseId;
        public float Wear => wear;
        public bool IsActive => active;
        public float InteractRange => interactRange;
        public bool IsPlaying { get; private set; }

        /// <summary>재생이 시작될 때 (퍼즐이 순서를 기록한다)</summary>
        public event System.Action<EchoCrease> Awakened;

        private static Material ghostMat;

        public bool TryAwaken(Vector3 from)
        {
            if (!active || IsPlaying) return false;
            if (Vector3.Distance(from, transform.position) > interactRange) return false;

            Awakened?.Invoke(this);
            StartCoroutine(Play());
            return true;
        }

        private IEnumerator Play()
        {
            IsPlaying = true;

            GameObject ghost = MakeGhost();
            float t = 0f;

            while (t < playSeconds)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / playSeconds);

                ghost.transform.position = SampledPath(u);

                // 나타났다 사라지는 페이드 (양 끝 15%)
                float a = Mathf.Min(u / 0.15f, (1f - u) / 0.15f, 1f);
                SetGhostAlpha(ghost, Mathf.Clamp01(a) * 0.55f);

                yield return null;
            }

            Destroy(ghost);
            IsPlaying = false;
        }

        private Vector3 PathFallback() => transform.position + Vector3.up * 0.5f;

        private Vector3 SampledPath(float u)
        {
            if (path == null || path.Length == 0) return PathFallback();
            if (path.Length == 1) return path[0].position;

            float f = u * (path.Length - 1);
            int i = Mathf.Clamp(Mathf.FloorToInt(f), 0, path.Length - 2);
            return Vector3.Lerp(path[i].position, path[i + 1].position, f - i);
        }

        private static GameObject MakeGhost()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "EchoGhost";
            go.transform.localScale = new Vector3(0.4f, 0.45f, 0.4f);
            Destroy(go.GetComponent<Collider>());

            if (ghostMat == null)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                ghostMat = new Material(sh);
            }

            var mr = go.GetComponent<MeshRenderer>();
            mr.material = new Material(ghostMat);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // 잔상은 그림자를 만들지 않는다
            return go;
        }

        private static void SetGhostAlpha(GameObject ghost, float a)
        {
            var mr = ghost.GetComponent<MeshRenderer>();
            // 잔상은 현재보다 채도를 더 뺀 색 — 과거임을 색으로 구분
            Color c = new(0.82f, 0.80f, 0.76f, a);
            if (mr.material.HasProperty("_BaseColor")) mr.material.SetColor("_BaseColor", c);
            else mr.material.color = c;
        }

        private void OnDrawGizmos()
        {
            if (!active) return;
            Gizmos.color = new Color(0.95f, 0.78f, 0.47f, 0.6f);
            Gizmos.DrawWireCube(transform.position, new Vector3(1.2f, 0.15f, 0.3f));
        }
    }
}
