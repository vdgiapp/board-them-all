using System;
using System.Collections.Generic;
using Gustom.References;
using Game.UI;
using UnityEngine;

namespace Game.Manager
{
    [DisallowMultipleComponent]
    public class UIManager : MonoBehaviourSingleton<UIManager>
    {
        [Header("Settings")]
        [SerializeField] private ResourceFolderReference uiResourceFolder;
        
        [field: Header("References")]
        [field: SerializeField] public Transform popupContainer { get; private set; }
        [field: SerializeField] public ActionDialog actionDialog { get; private set; }
        [field: SerializeField] public TextDialog textDialog { get; private set; }
        
        private readonly Dictionary<string, PopupWindow> _popupInstances = new(StringComparer.Ordinal);
        private string _cachedFolderPath;

        protected override void Awake()
        {
            base.Awake();
            string folderStr = uiResourceFolder; 
            if (!string.IsNullOrEmpty(folderStr))
            {
                _cachedFolderPath = folderStr.EndsWith("/") ? folderStr : folderStr + "/";
            }
            else
            {
                _cachedFolderPath = string.Empty;
            }
        }

        private PopupWindow GetPopupWindow(string popupName)
        {
            if (string.IsNullOrEmpty(popupName)) return null;
            if (!_popupInstances.TryGetValue(popupName, out PopupWindow popup))
            {
                string fullPath = _cachedFolderPath + popupName;
                GameObject prefab = Resources.Load<GameObject>(fullPath);
                if (!prefab) return null;

                GameObject go = Instantiate(prefab, popupContainer);
                popup = go.GetComponent<PopupWindow>();
                if (!popup)
                {
                    Destroy(go);
                    return null;
                }
                _popupInstances.Add(popupName, popup);
            }
            return popup;
        }

        public T GetPopup<T>(string popupName) where T : PopupWindow
        {
            PopupWindow popup = GetPopupWindow(popupName);
            if (popup is T typedPopup) return typedPopup;
            return null;
        }
    }
}