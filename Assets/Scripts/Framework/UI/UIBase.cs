using System.Threading.Tasks;
using UnityEngine;

namespace Game.Framework.UI
{
    [RequireComponent(typeof(RectTransform))]
    public abstract class UIBase : MonoBehaviour
    {
        private CanvasGroup canvasGroup;
        private bool isInitialized;

        public bool IsVisible { get; private set; }
        public RectTransform RectTransform { get; private set; }

        public async Task InitializeAsync()
        {
            if (isInitialized)
            {
                return;
            }

            RectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            await OnInitializeAsync();
            isInitialized = true;
        }

        public async Task ShowAsync()
        {
            await InitializeAsync();
            SetVisible(true);
            await OnShowAsync();
        }

        public async Task RefreshAsync()
        {
            await InitializeAsync();
            await OnRefreshAsync();
        }

        public async Task HideAsync()
        {
            if (!isInitialized)
            {
                return;
            }

            await OnHideAsync();
            SetVisible(false);
        }

        public virtual void Release()
        {
        }

        protected virtual Task OnInitializeAsync()
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnShowAsync()
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnRefreshAsync()
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnHideAsync()
        {
            return Task.CompletedTask;
        }

        protected void SetInteractable(bool interactable)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.interactable = interactable;
            canvasGroup.blocksRaycasts = interactable;
        }

        private void SetVisible(bool visible)
        {
            IsVisible = visible;
            gameObject.SetActive(visible);

            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
    }
}
