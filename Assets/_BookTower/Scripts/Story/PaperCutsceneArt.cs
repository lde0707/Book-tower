using UnityEngine;

namespace BookTower.Story
{
    [CreateAssetMenu(menuName = "Book Tower/Paper cutscene art")]
    public sealed class PaperCutsceneArt : ScriptableObject
    {
        public Sprite spirit, moa, foyer, theatre, memory;
        public Sprite[] entryCuts, returnCuts, theatreCuts, nameCuts;
        public Sprite[] firstPersonAwakening;
        public Sprite continuousBird, continuousWing, continuousHands, continuousSeed;
        public Sprite continuousOffering;
        public Sprite spiritPortrait;
        public bool HasContinuous => continuousBird && continuousOffering && continuousHands && continuousSeed;
        public bool HasFirstPerson => firstPersonAwakening != null && firstPersonAwakening.Length == 4 &&
            firstPersonAwakening[0] && firstPersonAwakening[1] && firstPersonAwakening[2] && firstPersonAwakening[3];
        public Sprite Frame(string sequence, int index)
        {
            var frames = sequence == OpeningStoryData.Promise ? entryCuts :
                sequence == OpeningStoryData.Return ? returnCuts :
                sequence == OpeningStoryData.Theatre ? theatreCuts :
                sequence == OpeningStoryData.Name ? nameCuts : null;
            return frames != null && index >= 0 && index < frames.Length ? frames[index] : null;
        }
        public bool IsReady => spirit && moa && foyer && theatre && memory;
    }
}
