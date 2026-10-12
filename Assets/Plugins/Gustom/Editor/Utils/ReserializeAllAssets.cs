#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Gustom.Editor
{
#if UNITY_6000_0_OR_NEWER
    public static class ReserializeAllAssets
    {
        [MenuItem("Assets/Force Reserialize All Assets")]
        public static void ReserializeAll()
        {
            if (EditorUtility.DisplayDialog("Confirmation", "Are you sure you want to upgrade and rewrite assets in this project? This process may take a while.", "Yes", "Cancel"))
            {
                Debug.Log("Starting force reserialize process...");
                AssetDatabase.ForceReserializeAssets();
                AssetDatabase.SaveAssets();
                Debug.Log("Successfully force reserialized all assets!");
            }
        }
    }
#endif
}
#endif
