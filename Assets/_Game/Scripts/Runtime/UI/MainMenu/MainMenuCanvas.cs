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
        [SerializeField] private int initialNavigationIndex = 2;
        private int currentNavigationIndex;

        private void Start()
        {
            SetNavigationTab(initialNavigationIndex);
        }

        public void SetNavigationTab(int idx)
        {
            for (int i = 0; i < navigationSettings.Length; i++)
            {
                NavigationSetting s = navigationSettings[i];
                s.button.PlayActivateAnimation(i == idx);
                s.tab.OnActivate(i == idx);
            }
            currentNavigationIndex = idx;
        }
    }
}