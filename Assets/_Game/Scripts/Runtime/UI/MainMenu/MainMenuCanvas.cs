using System;
using UnityEngine;

namespace Game.UI.MainMenu
{
    public class MainMenuCanvas : MonoBehaviour
    {
        [Serializable]
        public struct NavigationSetting
        {
            public NavigationButton button;
            public NavigationTab tab;
        }
        
        [SerializeField] private NavigationSetting[] navigationSettings;

        private int currentNavigationIndex;

        private void Start()
        {
            SetNavigationTab(2);
        }

        public void SetNavigationTab(int idx)
        {
            for (int i = 0; i < navigationSettings.Length; i++)
            {
                navigationSettings[i].button.PlayActivated(i == idx);
            }
        }
    }
}