using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using BookTower.Presentation;
using Object = UnityEngine.Object;

namespace BookTower.EditorTools
{
    public static class PaperAtmospherePolish
    {
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity"); Apply(); PaperAtmosphereReview.Run();
        }
        public static void Apply()
        {
            var profiles = Object.FindObjectsByType<PaperAtmosphere>(FindObjectsSortMode.None).OrderBy(p => p.room.RoomId).ToArray();
            var director = Object.FindFirstObjectByType<PaperAtmosphereDirector>();
            var calibration = director.GetComponent<PaperIrradianceCalibration>(); if (!calibration) calibration = director.gameObject.AddComponent<PaperIrradianceCalibration>();
            calibration.rooms = profiles; calibration.gains = profiles.Select(p => p.room.RoomId == "F0" ? .035f : .045f).ToArray();
            foreach (var p in profiles)
            {
                if (p.room.RoomId == "1B") { p.density = .013f; p.air = new(.062f, .086f, .125f); p.shaftDensity = .006f; }
                if (p.room.RoomId == "1A") { p.density = .010f; p.air = new(.13f, .18f, .17f); }
                if (p.room.RoomId == "F0") { p.density = .016f; p.shaftDensity = .020f; }
                EditorUtility.SetDirty(p);
            }
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
            foreach (var ao in renderer.rendererFeatures.Where(f => f.GetType().Name == "ScreenSpaceAmbientOcclusion"))
            {
                var so = new SerializedObject(ao); so.FindProperty("m_Settings.Intensity").floatValue = .48f;
                so.FindProperty("m_Settings.Radius").floatValue = .36f; so.FindProperty("m_Settings.DirectLightingStrength").floatValue = .10f;
                so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(ao);
            }
            EditorUtility.SetDirty(calibration); EditorUtility.SetDirty(renderer); renderer.SetDirty();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
        }
    }
}
