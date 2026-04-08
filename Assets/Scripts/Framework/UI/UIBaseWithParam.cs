using System.Threading.Tasks;

namespace Game.Framework.UI
{
    public abstract class UIBase<TParam> : UIBase where TParam : UIParam
    {
        protected TParam Param { get; private set; }

        public async Task ShowAsync(TParam param)
        {
            await InitializeAsync();
            await ApplyParamAsync(param);
            await base.ShowAsync();
        }

        public async Task RefreshAsync(TParam param)
        {
            await InitializeAsync();
            await ApplyParamAsync(param);
            await base.RefreshAsync();
        }

        protected virtual Task OnApplyParamAsync(TParam param)
        {
            return Task.CompletedTask;
        }

        private async Task ApplyParamAsync(TParam param)
        {
            Param = param;
            await OnApplyParamAsync(param);
        }
    }
}
