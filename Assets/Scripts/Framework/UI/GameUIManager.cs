using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Framework.ResourceManagement;
using UnityEngine;

namespace Game.Framework.UI
{
    public sealed class GameUIManager : MonoBehaviour
    {
        private const string InstanceName = "[GameUIManager]";

        private readonly Stack<UIBase> popupStack = new Stack<UIBase>();
        private readonly HashSet<UIBase> openedViews = new HashSet<UIBase>();

        private UIBase currentPage;
        private UIRoot root;

        public static GameUIManager Instance { get; private set; }

        public UIBase CurrentPage => currentPage;
        public int PopupCount => popupStack.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateInstance()
        {
            if (Instance != null)
            {
                return;
            }

            var gameObject = new GameObject(InstanceName);
            DontDestroyOnLoad(gameObject);
            gameObject.AddComponent<GameUIManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            root = UIRoot.Ensure();
        }

        public async Task<TPage> OpenPageAsync<TPage>(UIKey key) where TPage : UIBase, IUIPage
        {
            var page = await CreateUIAsync<TPage>(key, root.PageLayer);

            if (currentPage != null)
            {
                await CloseUIAsync(currentPage);
            }

            currentPage = page;
            await page.ShowAsync();
            return page;
        }

        public async Task<TPage> OpenPageAsync<TPage, TParam>(UIKey key, TParam param)
            where TPage : UIBase<TParam>, IUIPage
            where TParam : UIParam
        {
            var page = await CreateUIAsync<TPage>(key, root.PageLayer);

            if (currentPage != null)
            {
                await CloseUIAsync(currentPage);
            }

            currentPage = page;
            await page.ShowAsync(param);
            return page;
        }

        public async Task<TPopup> OpenPopupAsync<TPopup>(UIKey key) where TPopup : UIBase, IUIPopup
        {
            var popup = await CreateUIAsync<TPopup>(key, root.PopupLayer);
            popupStack.Push(popup);
            await popup.ShowAsync();
            return popup;
        }

        public async Task<TPopup> OpenPopupAsync<TPopup, TParam>(UIKey key, TParam param)
            where TPopup : UIBase<TParam>, IUIPopup
            where TParam : UIParam
        {
            var popup = await CreateUIAsync<TPopup>(key, root.PopupLayer);
            popupStack.Push(popup);
            await popup.ShowAsync(param);
            return popup;
        }

        public async Task<TView> CreateViewAsync<TView>(UIKey key, Transform parent = null)
            where TView : UIBase, IUIView
        {
            var view = await CreateUIAsync<TView>(key, parent != null ? parent : root.ViewLayer);
            openedViews.Add(view);
            await view.ShowAsync();
            return view;
        }

        public async Task<TView> CreateViewAsync<TView, TParam>(UIKey key, TParam param, Transform parent = null)
            where TView : UIBase<TParam>, IUIView
            where TParam : UIParam
        {
            var view = await CreateUIAsync<TView>(key, parent != null ? parent : root.ViewLayer);
            openedViews.Add(view);
            await view.ShowAsync(param);
            return view;
        }

        public async void CloseTopPopup()
        {
            if (popupStack.Count <= 0)
            {
                return;
            }

            var popup = popupStack.Pop();
            await CloseUIAsync(popup);
        }

        public async void ClosePopup(UIBase popup)
        {
            if (popup == null)
            {
                return;
            }

            if (popupStack.Count > 0 && ReferenceEquals(popupStack.Peek(), popup))
            {
                popupStack.Pop();
                await CloseUIAsync(popup);
                return;
            }

            var popups = popupStack.ToArray();
            popupStack.Clear();

            for (var i = popups.Length - 1; i >= 0; i--)
            {
                if (ReferenceEquals(popups[i], popup))
                {
                    await CloseUIAsync(popup);
                    continue;
                }

                popupStack.Push(popups[i]);
            }
        }

        public async Task CloseAllPopupsAsync()
        {
            while (popupStack.Count > 0)
            {
                await CloseUIAsync(popupStack.Pop());
            }
        }

        public async Task ReleaseViewAsync(UIBase view)
        {
            if (view == null || !openedViews.Remove(view))
            {
                return;
            }

            await CloseUIAsync(view);
        }

        private async Task<TUI> CreateUIAsync<TUI>(UIKey key, Transform parent) where TUI : UIBase
        {
            root = root != null ? root : UIRoot.Ensure();
            var instance = await GameResourceManager.Instance.InstantiateAsync(key.Value, parent);
            var ui = instance.GetComponent<TUI>();

            if (ui == null)
            {
                GameResourceManager.Instance.ReleaseInstance(instance);
                throw new InvalidOperationException($"UI prefab does not contain component {typeof(TUI).Name}: {key.Value}");
            }

            StretchToParent(ui.RectTransform != null ? ui.RectTransform : ui.GetComponent<RectTransform>());
            await ui.InitializeAsync();
            return ui;
        }

        private static async Task CloseUIAsync(UIBase ui)
        {
            await ui.HideAsync();
            ui.Release();
            GameResourceManager.Instance.ReleaseInstance(ui.gameObject);
        }

        private static void StretchToParent(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;
        }
    }
}
