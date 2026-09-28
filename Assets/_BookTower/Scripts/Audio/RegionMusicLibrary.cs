using UnityEngine;
namespace BookTower.Audio
{
    public sealed class RegionMusicLibrary : ScriptableObject
    {
        public AudioClip[] clips;
        [Range(0,1)] public float volume=.5f;
        [Min(.1f)] public float fadeSeconds=2.5f;
    }
}
