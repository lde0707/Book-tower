using System.IO;
using UnityEditor;
using UnityEngine;

namespace BookTower.EditorTools
{
    public static partial class InteriorF0Builder
    {
        const string IdentityFolder = "Assets/_BookTower/Generated/F0PaperIdentity";
        const string IdentityTextures = "Assets/_BookTower/Textures/F0PaperIdentity";
        static Material identityFoil, identityWindow, identityBeam, identityGold, identityShade;

        static void IdentityMaterials()
        {
            EnsureFolder(IdentityFolder);
            string source = IdentityTextures + "/MulberryFiber.png";
            string normal = IdentityTextures + "/MulberryFiberNormal.png";
            // Unity derives a normal map from the same source; the original art remains untouched.
            if (!File.Exists(normal)) File.Copy(source, normal);
            AssetDatabase.ImportAsset(source, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(normal, ImportAssetOptions.ForceSynchronousImport);
            foreach (string path in new[] { source, normal })
            {
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                imp.textureType = path == normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                imp.convertToNormalmap = path == normal;
                imp.heightmapScale = .12f;
                imp.normalmapFilter = TextureImporterNormalFilter.Standard;
                imp.wrapMode = TextureWrapMode.Mirror;
                imp.anisoLevel = 8;
                imp.maxTextureSize = 2048;
                imp.textureCompression = TextureImporterCompression.CompressedHQ;
                imp.mipmapEnabled = true;
                imp.sRGBTexture = path != normal;
                imp.SaveAndReimport();
            }
            paper = IdentityMaterial("WarmMulberry", new Color(.955f,.892f,.755f), .10f, 0, .60f);
            ivory = IdentityMaterial("IvoryMulberry", new Color(.98f,.92f,.80f), .10f, 0, .65f);
            edge = IdentityMaterial("LayeredCottonEdges", new Color(.72f,.62f,.46f), .07f, 0, .40f);
            oxblood = IdentityMaterial("RustDyedPaper", new Color(.42f,.12f,.062f), .15f, 0, .50f);
            umber = IdentityMaterial("UmberDyedPaper", new Color(.23f,.13f,.071f), .12f, 0, .40f);
            ochre = IdentityMaterial("OchreDyedPaper", new Color(.50f,.30f,.12f), .14f, 0, .45f);
            wood = IdentityMaterial("InkBinding", new Color(.12f,.090f,.058f), .16f, 0, .25f);
            ink = IdentityMaterial("TerracottaPrint", new Color(.40f,.15f,.082f), .08f, 0, .18f);
            rope = IdentityMaterial("CottonThread", new Color(.86f,.77f,.60f), .08f, 0, .50f);
            brass = IdentityMaterial("DullBindingBrass", new Color(.57f,.35f,.12f), .32f, .50f, .13f);
            identityGold = IdentityMaterial("PressedLeafGold", new Color(.64f,.41f,.16f), .23f, .22f, .17f);
            identityShade = IdentityMaterial("PaperShadow", new Color(.28f,.21f,.14f), .08f, 0, .20f);
            identityFoil = IdentityMaterial("SageSilverFoil", new Color(.59f,.70f,.67f), .61f, .68f, .12f);
            glow = IdentityMaterial("LanternMulberry", new Color(1,.79f,.50f), .08f, 0, .20f, .8f);
            identityWindow = IdentityMaterial("WindowMulberry", new Color(1,.86f,.65f), .08f, 0, .18f, .65f);
            identityBeam = IdentityMaterial("HoneyOpticalLight", new Color(1,.73f,.34f), .1f, 0, 0, 2.2f);
        }

        static Material IdentityMaterial(string name, Color color, float smooth, float metal, float bump, float emission = 0)
        {
            string path = IdentityFolder + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat,path); }
            mat.SetColor("_BaseColor",color);
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(IdentityTextures+"/MulberryFiber.png"));
            mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(IdentityTextures+"/MulberryFiberNormal.png"));
            mat.SetFloat("_BumpScale",bump); mat.EnableKeyword("_NORMALMAP");
            mat.SetFloat("_Smoothness",smooth); mat.SetFloat("_Metallic",metal);
            mat.SetFloat("_Cull",0); mat.SetFloat("_EnvironmentReflections", metal > 0 ? 1 : 0);
            if(metal>0)mat.DisableKeyword("_ENVIRONMENTREFLECTIONS_OFF");else mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            mat.SetColor("_EmissionColor",color*emission);
            mat.globalIlluminationFlags=emission>0?MaterialGlobalIlluminationFlags.RealtimeEmissive:MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            if(emission>0)mat.EnableKeyword("_EMISSION");else mat.DisableKeyword("_EMISSION");
            mat.enableInstancing=true;EditorUtility.SetDirty(mat);return mat;
        }

        static Material IdentityReplacement(Material original)
        {
            if (!original) return ivory;
            string n = original.name;
            if (n.Contains("Silver") || n.Contains("Foil")) return identityFoil;
            if (n.Contains("Lantern")) return glow;
            if (n.Contains("Brass")) return brass;
            if (n.Contains("Rope") || n.Contains("Thread")) return rope;
            if (n.Contains("Terracotta")) return ink;
            if (n.Contains("DarkBinding")) return wood;
            if (n.Contains("Oxblood") || n.Contains("Leather")) return oxblood;
            if (n.Contains("Umber")) return umber;
            if (n.Contains("Ochre")) return ochre;
            if (n.Contains("Layered") || n.Contains("Edges")) return edge;
            if (n.Contains("WarmPaper")) return paper;
            if (n.Contains("Ivory")) return ivory;
            return original;
        }
    }
}
