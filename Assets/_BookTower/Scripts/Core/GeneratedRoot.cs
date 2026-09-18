using UnityEngine;

namespace BookTower.Core
{
    /// <summary>
    /// 씬 구성기가 만든 루트라는 표식.
    ///
    /// 왜 이름 대신 표식인가
    ///   예전엔 청소할 루트를 **이름 목록**으로 골랐다. 그런데 목록에
    ///   "Map" 은 있고 "Map (히든)" 은 없었다. 그래서 씬을 다시 구울 때마다
    ///   히든 루트만 안 지워지고 계속 쌓였다 — 실제로 **10개**가 겹쳐 있었다.
    ///
    ///   겹친 사본은 그냥 낭비가 아니라 버그다. RoomManager 는
    ///   FindObjectsByType&lt;Room&gt; 으로 방을 찾는데, 사본이 10개면 방도 10개씩
    ///   잡힌다. 룸 컬링이 그중 하나만 끄니 나머지가 화면에 그대로 남아
    ///   "다른 길이나 지형이 시야를 가린다" 가 된다.
    ///
    ///   이름이 하나라도 바뀌거나 늘면 같은 일이 또 생긴다. 만든 쪽이
    ///   직접 표식을 남기면 목록을 관리할 필요가 없어진다.
    /// </summary>
    [DisallowMultipleComponent]
    public class GeneratedRoot : MonoBehaviour { }
}
