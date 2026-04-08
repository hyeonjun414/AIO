namespace Game.Framework.UI
{
    public interface IUIView
    {
    }

    public abstract class UIView : UIBase, IUIView
    {
    }

    public abstract class UIView<TParam> : UIBase<TParam>, IUIView where TParam : UIParam
    {
    }
}
