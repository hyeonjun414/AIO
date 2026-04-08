using UnityEngine;
using UnityEngine.UI;

namespace Game.Framework.UI
{
    public sealed class UIRoot : MonoBehaviour
    {
        private const string RootName = "[GameUIRoot]";

        public static UIRoot Instance { get; private set; }

        public RectTransform PageLayer { get; private set; }
        public RectTransform ViewLayer { get; private set; }
        public RectTransform PopupLayer { get; private set; }
        public RectTransform ToastLayer { get; private set; }
        public RectTransform SystemLayer { get; private set; }

        public static UIRoot Ensure()
        {
            if (Instance != null)
            {
                return Instance;
            }

            var rootObject = new GameObject(RootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(rootObject);

            var canvas = rootObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var scaler = rootObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return rootObject.AddComponent<UIRoot>();
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

            PageLayer = CreateLayer("PageLayer");
            ViewLayer = CreateLayer("ViewLayer");
            PopupLayer = CreateLayer("PopupLayer");
            ToastLayer = CreateLayer("ToastLayer");
            SystemLayer = CreateLayer("SystemLayer");
        }

        public RectTransform GetLayer(UILayer layer)
        {
            return layer switch
            {
                UILayer.Page => PageLayer,
                UILayer.View => ViewLayer,
                UILayer.Popup => PopupLayer,
                UILayer.Toast => ToastLayer,
                UILayer.System => SystemLayer,
                _ => PageLayer
            };
        }

        private RectTransform CreateLayer(string layerName)
        {
            var layerObject = new GameObject(layerName, typeof(RectTransform));
            var rectTransform = layerObject.GetComponent<RectTransform>();
            rectTransform.SetParent(transform, false);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;
            return rectTransform;
        }
    }
}
