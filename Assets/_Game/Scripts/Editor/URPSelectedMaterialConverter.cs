#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Game.Editor
{
    #if UNITY_6000_3_OR_NEWER
    public static class URPSelectedMaterialConverter
    {
        [MenuItem("Assets/Convert Selected Materials to URP", false, 2000)]
        public static void ConvertSelectedMaterials()
        {
            List<Material> selectedMaterials = new List<Material>();

            foreach (Object obj in Selection.objects)
            {
                if (obj is Material mat)
                {
                    selectedMaterials.Add(mat);
                }
            }

            if (selectedMaterials.Count == 0)
            {
                EditorUtility.DisplayDialog("No Materials Selected", "Please select at least one Built-in material in the Project view.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Convert Materials", $"Are you sure you want to upgrade {selectedMaterials.Count} selected material(s) to URP Lit?", "Yes", "Cancel"))
            {
                return;
            }

            Undo.RecordObjects(selectedMaterials.ToArray(), "Convert Materials to URP");

            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLitShader == null)
            {
                Debug.LogError("Could not find the 'Universal Render Pipeline/Lit' shader. Ensure URP is installed.");
                return;
            }

            int convertedCount = 0;
            foreach (Material mat in selectedMaterials)
            {
                // Simple mapping from Standard Albedo to URP Base Map
                Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                Color mainColor = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;

                mat.shader = urpLitShader;

                // Re-apply properties to URP shader map names
                if (mainTex != null && mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", mainTex);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", mainColor);

                EditorUtility.SetDirty(mat);
                convertedCount++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Successfully upgraded {convertedCount} materials to URP Lit.");
        }
    }
    #endif
}
#endif
