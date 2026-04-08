namespace Game.Framework.UI
{
    public static class UIKeys
    {
        public const string PagePrefix = "UI/Page/";
        public const string PopupPrefix = "UI/Popup/";
        public const string ViewPrefix = "UI/View/";

        public static UIKey Page(string name)
        {
            return new UIKey(PagePrefix + name);
        }

        public static UIKey Popup(string name)
        {
            return new UIKey(PopupPrefix + name);
        }

        public static UIKey View(string name)
        {
            return new UIKey(ViewPrefix + name);
        }
    }
}
