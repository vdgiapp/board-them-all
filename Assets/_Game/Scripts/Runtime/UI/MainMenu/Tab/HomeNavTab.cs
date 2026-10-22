namespace Game.UI.MainMenu
{
    public class HomeNavTab : NavigationTab
    {
        public override void OnActivate(bool activate)
        {
            gameObject.SetActive(activate);
        }
    }
}