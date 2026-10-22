using UnityEngine;

namespace Game.UI.MainMenu
{
    public abstract class NavigationTab : MonoBehaviour
    {
        public abstract void OnActivate(bool activate);
    }
}