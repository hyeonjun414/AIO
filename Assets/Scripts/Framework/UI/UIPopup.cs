namespace Game.Framework.UI
{
    public interface IUIPopup
    {
    }

    public abstract class UIPopup : UIBase, IUIPopup
    {
        protected void CloseSelf()
        {
            GameUIManager.Instance.ClosePopup(this);
        }
    }

    public abstract class UIPopup<TParam> : UIBase<TParam>, IUIPopup where TParam : UIParam
    {
        protected void CloseSelf()
        {
            GameUIManager.Instance.ClosePopup(this);
        }
    }
}
