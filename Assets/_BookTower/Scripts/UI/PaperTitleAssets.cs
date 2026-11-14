using UnityEngine;

namespace BookTower.UI
{
    public sealed class PaperTitleAssets : ScriptableObject
    {
        public Texture2D background;
        public Sprite menuPaper, menuSelected, menuLeaf;
        public Shader motionShader;
        public AudioClip music;
        public Font titleFont;
    }
}
