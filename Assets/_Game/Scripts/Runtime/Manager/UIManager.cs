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
        
        private readonly Dictionary<string, PopupWindow> _popupInstances = new();
        
        private PopupWindow GetPopupWindow(string popupName)
        {
            if (string.IsNullOrEmpty(popupName)) return null;
            if (!_popupInstances.TryGetValue(popupName, out PopupWindow popup))
            {
                GameObject go = Instantiate(Resources.Load<GameObject>(uiResourceFolder + popupName), popupContainer);
                popup = go.GetComponent<PopupWindow>();
                _popupInstances.Add(popupName, popup);
            }
            return popup;
        }

        // public PopupReward ShowPopupReward(PackageResource packageResource)
        // {
        //     PopupReward popup = GetPopupWindow(nameof(PopupReward)) as PopupReward;
        //     popup.Show(packageResource);
        //     return popup;
        // }
    }
}