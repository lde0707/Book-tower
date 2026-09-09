using UnityEngine;

namespace BookTower.Core
{
    /// <summary>
    /// 씬 오브젝트에 기획 의도를 붙여두는 메모.
    ///
    /// 레벨을 만들다 보면 "이걸 왜 여기 놨더라"를 반드시 잊게 된다.
    /// 문서와 씬이 따로 놀지 않게, 의도를 오브젝트 자체에 남긴다.
    /// 런타임에는 아무 일도 하지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    public class LevelNote : MonoBehaviour
    {
        [TextArea(2, 6)]
        [SerializeField] private string note;

        public string Text => note;

        private void OnDrawGizmos()
        {
            if (string.IsNullOrEmpty(note)) return;

            Gizmos.color = new Color(0.95f, 0.78f, 0.47f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, 0.4f);
        }
    }
}
