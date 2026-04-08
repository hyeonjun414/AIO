namespace Game.Framework.UI
{
    public interface IUIPage
    {
    }

    public abstract class UIPage : UIBase, IUIPage
    {
    }

    public abstract class UIPage<TParam> : UIBase<TParam>, IUIPage where TParam : UIParam
    {
    }
}
