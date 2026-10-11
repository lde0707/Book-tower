using UnityEngine;
using BookTower.Rooms;
using BookTower.UI;

namespace BookTower.Puzzles
{
    public sealed partial class GravityPageBuildProbe
    {
        void VerifySettingsContract()
        {
            var original = new PaperSettings();
            var copy = original.Copy();
            Check(original.Same(copy), "settings copy preserves all values");
            copy.keys[0]++;
            Check(original.keys[0] != copy.keys[0] && !original.Same(copy), "editing draft keys cannot mutate saved settings");
            // A newly added setting must be represented in equality, or this regression fails.
            foreach (var field in typeof(PaperSettings).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
            {
                copy = original.Copy();
                if (field.FieldType == typeof(int)) field.SetValue(copy, (int)field.GetValue(copy) + 1);
                else if (field.FieldType == typeof(float)) field.SetValue(copy, (float)field.GetValue(copy) + .25f);
                else if (field.FieldType == typeof(bool)) field.SetValue(copy, !(bool)field.GetValue(copy));
                else if (field.FieldType == typeof(int[])) copy.keys[0]++;
                else { Check(false, "add settings equality coverage for " + field.Name); continue; }
                Check(!original.Same(copy), "settings equality includes " + field.Name);
            }
        }
        void VerifyVisibilityContract()
        {
            var owner = new GameObject("QA visibility contract");
            try
            {
                var surface = owner.AddComponent<MeshRenderer>();
                var blocker = owner.AddComponent<BoxCollider>();
                var authoredOff = new GameObject("Authored disabled light");
                authoredOff.transform.SetParent(owner.transform);
                var light = authoredOff.AddComponent<Light>();
                light.enabled = false;
                var visibility = new RoomVisibility(owner.transform);
                visibility.SetVisible(false);
                Check(!surface.enabled && blocker.enabled, "visibility hides rendering without disabling collision");
                visibility.SetVisible(false);
                visibility.SetVisible(true);
                Check(surface.enabled && !light.enabled, "repeated hide preserves authored disabled light");
                surface.enabled = false;
                light.enabled = true;
                visibility.SetVisible(false);
                visibility.SetVisible(true);
                Check(!surface.enabled && light.enabled, "visibility restores latest local renderer/light states");
            }
            finally { Destroy(owner); }
        }
    }
}
