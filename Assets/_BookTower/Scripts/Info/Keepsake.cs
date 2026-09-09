using System;
using UnityEngine;

namespace BookTower.Info
{
    /// <summary>모은 단서 하나. 유품함에 보관되어 언제든 다시 볼 수 있다</summary>
    [Serializable]
    public struct Keepsake
    {
        public string id;
        public string label;
        /// <summary>어느 구역에서 얻었는지 (역참조 유도용)</summary>
        public string areaId;
        /// <summary>가짜 단서인가 (레드 헤링)</summary>
        public bool isDecoy;
    }
}
