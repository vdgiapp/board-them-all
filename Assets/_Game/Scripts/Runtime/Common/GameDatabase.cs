using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game
{
    /// <summary>
    /// Marker interface. Any ScriptableObject implementing this is auto-registered into GameDatabase.
    /// </summary>
    public interface IGameModuleData { }

    /// <summary>
    /// Central database for static game data modules. Runtime access via <see cref="GetModule{T}"/>.
    /// Editor tooling keeps the module list in sync automatically.
    /// <para>
    /// Setup: create the asset with <c>Assets &gt; Create &gt; Game &gt; Game Database</c>. It must be named
    /// <c>GameDatabase</c> and live inside a <c>Resources</c> folder, because it is loaded with
    /// <see cref="Resources.Load{T}(string)"/>.
    /// </para>
    /// <para>
    /// Modules are looked up by their exact runtime type, so request the concrete class
    /// (<c>GetModule&lt;ItemData&gt;()</c>), not a base class or interface.
    /// </para>
    /// </summary>
    public sealed class GameDatabase : ScriptableObject
    {
        /// <summary>
        /// Registered modules. Filled by editor <see cref="Sync"/>; read-only in the Inspector.
        /// </summary>
        [SerializeField] private List<ScriptableObject> dataModules = new();

        /// <summary>
        /// Type-to-module map built lazily from <see cref="dataModules"/>.
        /// </summary>
        private Dictionary<Type, ScriptableObject> _lookup;

        private static GameDatabase instance;

        /// <summary>
        /// The database asset, loaded from <c>Resources/GameDatabase</c> on first access and cached.
        /// Returns <c>null</c> (and logs a warning) if the asset does not exist.
        /// </summary>
        public static GameDatabase Instance
        {
            get
            {
                if (!instance)
                {
                    GameDatabase loaded = Resources.Load<GameDatabase>(nameof(GameDatabase));
                    if (!loaded)
                    {
                        Debug.LogWarning("No database asset found. In the Project window, go to 'Create > Game > Game Database' to create one (it must be named 'GameDatabase' and sit inside a 'Resources' folder).");
                        return null;
                    }
                    instance = loaded;
                    instance._lookup = instance.BuildLookup();
                }
                return instance;
            }
        }

        /// <summary>
        /// Builds the type-to-module map. Null entries are skipped. If two modules share the same type,
        /// the first one wins and a warning is logged.
        /// </summary>
        private Dictionary<Type, ScriptableObject> BuildLookup()
        {
            Dictionary<Type, ScriptableObject> map = new(dataModules.Count);
            foreach (var m in dataModules)
            {
                if (!m) continue;
                if (!map.TryAdd(m.GetType(), m))
                {
                    Debug.LogWarning($"Duplicate module type '{m.GetType().Name}'. Using '{map[m.GetType()].name}', ignoring '{m.name}'.");
                }
            }
            return map;
        }

        /// <summary>
        /// Returns the registered module of exact type <typeparamref name="T"/>.
        /// O(1) dictionary lookup, no allocation after the first call.
        /// </summary>
        /// <typeparam name="T">The concrete module type. Must be a ScriptableObject implementing <see cref="IGameModuleData"/>.</typeparam>
        /// <returns>
        /// The module, or <c>null</c> if the database asset is missing (warning logged)
        /// or the module is not registered (error logged).
        /// </returns>
        public static T GetModule<T>() where T : ScriptableObject, IGameModuleData
        {
            GameDatabase db = Instance;
            if (!db) return null;

            db._lookup ??= db.BuildLookup();
            if (db._lookup.TryGetValue(typeof(T), out ScriptableObject module)) return (T)module;

            Debug.LogError($"Module '{typeof(T).Name}' is not registered.");
            return null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Inspector for <see cref="GameDatabase"/>: shows the module list read-only plus a manual sync button.
        /// </summary>
        [CustomEditor(typeof(GameDatabase))]
        public class GameDatabaseEditor : Editor
        {
            /// <inheritdoc/>
            public override void OnInspectorGUI()
            {
                serializedObject.Update();
                EditorGUILayout.HelpBox($"Every ScriptableObject implementing '{nameof(IGameModuleData)}' is registered here automatically (read-only).", MessageType.Info);
                using (new EditorGUI.DisabledScope(true)) EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(dataModules)), new GUIContent("Registered Data Modules"), true);
                if (GUILayout.Button("Sync Data Modules", GUILayout.Height(24))) Sync();
                serializedObject.ApplyModifiedProperties();
            }
        }

        /// <summary>
        /// Triggers a <see cref="Sync"/> (once, after the current editor tick) whenever a <c>.asset</c> file
        /// is deleted or moved, or an <see cref="IGameModuleData"/> asset is imported.
        /// </summary>
        public class GameDatabaseWatcher : AssetPostprocessor
        {
            /// <summary>True while a sync is already scheduled, so a burst of imports causes only one.</summary>
            private static bool _queued;

            private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
            {
                if (_queued || !Touched(imported, deleted, moved)) return;
                _queued = true;
                EditorApplication.delayCall += () => { _queued = false; Sync(); };
            }

            /// <summary>
            /// Whether this batch of asset changes can affect the module list: any deleted or moved
            /// <c>.asset</c>, or an imported <c>.asset</c> whose type implements <see cref="IGameModuleData"/>.
            /// </summary>
            private static bool Touched(string[] imported, string[] deleted, string[] moved)
            {
                foreach (var p in deleted) if (p.EndsWith(".asset")) return true;
                foreach (var p in moved) if (p.EndsWith(".asset")) return true;
                foreach (var p in imported)
                {
                    if (!p.EndsWith(".asset")) continue;
                    var type = AssetDatabase.GetMainAssetTypeAtPath(p);
                    if (type != null && typeof(IGameModuleData).IsAssignableFrom(type)) return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Menu command <c>Assets &gt; Create &gt; Game &gt; Game Database</c>.
        /// If a database already exists, selects it (and warns if it is outside a <c>Resources</c> folder).
        /// Otherwise creates one in the selected folder if that folder is named <c>Resources</c>,
        /// or at <c>Assets/Resources/GameDatabase.asset</c>. Then runs <see cref="Sync"/>.
        /// </summary>
        [MenuItem("Assets/Create/Game/Game Database", priority = 0)]
        public static void CreateOrFindScriptableObject()
        {
            GameDatabase asset = null;
            const string defaultPath = "Assets/Resources/GameDatabase.asset";

            string[] guids = AssetDatabase.FindAssets($"t:{nameof(GameDatabase)}");

            if (guids.Length > 0)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                asset = AssetDatabase.LoadAssetAtPath<GameDatabase>(assetPath);
                Debug.Log($"{nameof(GameDatabase)} found at: {assetPath}");
                if (!assetPath.Contains("/Resources/")) Debug.LogWarning($"{nameof(GameDatabase)} is not inside a Resources folder, runtime loading will fail.");
            }
            else
            {
                string targetPath;
                string clickedFolderPath = GetSelectedFolderPath();
                if (!string.IsNullOrEmpty(clickedFolderPath) && Path.GetFileName(clickedFolderPath).Equals("Resources", StringComparison.OrdinalIgnoreCase))
                {
                    targetPath = $"{clickedFolderPath}/GameDatabase.asset";
                }
                else
                {
                    targetPath = defaultPath;
                    Debug.LogWarning($"The currently selected folder is not Resources folder. Created {nameof(GameDatabase)} at {targetPath}.");
                }

                string directory = Path.GetDirectoryName(targetPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    AssetDatabase.Refresh();
                }

                asset = CreateInstance<GameDatabase>();
                AssetDatabase.CreateAsset(asset, targetPath);
                AssetDatabase.SaveAssets();
            }
            
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            Sync();
        }

        /// <summary>
        /// Folder of the first selected item in the Project window (the item's parent folder if it is a file),
        /// or an empty string if nothing is selected.
        /// </summary>
        private static string GetSelectedFolderPath()
        {
            if (Selection.assetGUIDs == null || Selection.assetGUIDs.Length == 0) return string.Empty;
            string path = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
            if (!Directory.Exists(path)) path = Path.GetDirectoryName(path)?.Replace('\\', '/');
            return path;
        }

        /// <summary>Syncs after every editor domain reload (script recompile), except when entering Play Mode.</summary>
        [InitializeOnLoadMethod]
        private static void OnDomainReload()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            EditorApplication.delayCall += () => Sync();
        }

        /// <summary>
        /// Editor only. Rebuilds the module list: finds every non-abstract ScriptableObject type implementing
        /// <see cref="IGameModuleData"/>, collects all assets of those types (sorted by path for a stable order),
        /// and stores them. The asset is marked dirty and saved only if the list actually changed.
        /// Runs automatically on domain reload and on relevant asset changes (see <see cref="GameDatabaseWatcher"/>),
        /// or manually from the Inspector button.
        /// </summary>
        /// <returns>The database, or <c>null</c> if no database asset exists.</returns>
        public static GameDatabase Sync()
        {
            if (!Instance) return null;

            List<ScriptableObject> found = TypeCache.GetTypesDerivedFrom<IGameModuleData>()
                .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition && typeof(ScriptableObject).IsAssignableFrom(t))
                .SelectMany(t => AssetDatabase.FindAssets($"t:{t.Name}"))
                .Distinct()
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(p => p, StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<ScriptableObject>)
                .Where(m => m is IGameModuleData)
                .ToList();

            if (!Instance.dataModules.SequenceEqual(found))
            {
                Instance.dataModules = found;
                Instance._lookup = null;
                EditorUtility.SetDirty(Instance);
                EditorApplication.delayCall += AssetDatabase.SaveAssets;
            }
            return Instance;
        }
#endif
    }
}