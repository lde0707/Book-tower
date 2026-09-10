using UnityEngine;

namespace BookTower.Presentation
{
    /// <summary>
    /// 연필로 그린 듯한 외곽선.
    ///
    /// 셰이더를 새로 쓰지 않고 '뒤집힌 껍질(inverted hull)' 방식을 쓴다.
    ///   1. 같은 메시를 살짝 키워서 자식으로 하나 더 만든다
    ///   2. 그 껍질의 재질은 앞면을 잘라내(_Cull = Front) 뒷면만 남긴다
    ///   3. 그러면 원래 물체에 가려지고 가장자리에만 어두운 테두리가 보인다
    ///
    /// 왜 이렇게 하나
    ///   화면 전체에 외곽선을 그리는 방식(Renderer Feature)이 더 예쁘지만
    ///   URP 설정을 건드려야 하고, 셰이더를 잘못 쓰면 프로젝트가 통째로
    ///   컴파일이 안 된다. 이 방식은 기본 셰이더만으로 돌아간다.
    ///
    /// 흰 바탕 위에서 이 선이 형태를 잡아 준다.
    /// 선이 없으면 흰 물체가 흰 배경에 묻혀서 아무것도 안 보인다.
    /// </summary>
    [DisallowMultipleComponent]
    public class PencilOutline : MonoBehaviour
    {
        [Tooltip("껍질을 얼마나 키울지. 물체 크기에 비례하지 않으므로 " +
                 "큰 면에서는 선이 상대적으로 가늘어 보인다")]
        [SerializeField] private float thickness = 0.035f;

        private const string ChildName = "외곽선";

        /// <summary>에디터에서 껍질을 만들어 붙인다. 런타임에는 아무 일도 하지 않는다.</summary>
        public static GameObject Attach(GameObject target, Material outlineMat, float thickness)
        {
            var mf = target.GetComponent<MeshFilter>();
            var mr = target.GetComponent<MeshRenderer>();
            if (mf == null || mr == null || outlineMat == null) return null;

            Transform old = target.transform.Find(ChildName);
            if (old != null) DestroyImmediate(old.gameObject);

            var shell = new GameObject(ChildName);
            shell.transform.SetParent(target.transform, false);

            // 두께를 월드 기준으로 맞춘다. 부모가 납작하게 눌려 있으면
            // 로컬 배율을 그대로 쓸 경우 한 축만 두꺼워져 선이 뭉개진다.
            Vector3 ls = target.transform.lossyScale;
            shell.transform.localScale = new Vector3(
                1f + thickness * 2f / Mathf.Max(ls.x, 0.001f),
                1f + thickness * 2f / Mathf.Max(ls.y, 0.001f),
                1f + thickness * 2f / Mathf.Max(ls.z, 0.001f));

            shell.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;

            var sr = shell.AddComponent<MeshRenderer>();
            sr.sharedMaterial = outlineMat;
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sr.receiveShadows = false;

            var comp = target.GetComponent<PencilOutline>();
            if (comp == null) comp = target.AddComponent<PencilOutline>();
            comp.thickness = thickness;

            return shell;
        }
    }
}
