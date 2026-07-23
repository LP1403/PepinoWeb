using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PepinoGame.UI
{
    /// <summary>
    /// Full-screen Pepineado bang: title + "{name} te pepineó" (or "¡Pepineaste!").
    /// </summary>
    public static class PepineadoOverlay
    {
        private const string RootName = "PepineadoOverlayRuntime";

        private static readonly Color PanelBg = new Color(0.05f, 0.08f, 0.06f, 0.82f);
        private static readonly Color Accent = new Color(0.35f, 0.85f, 0.4f, 1f);
        private static readonly Color Cream = new Color(0.98f, 0.96f, 0.9f, 1f);
        private static readonly Color Gold = new Color(1f, 0.9f, 0.35f, 1f);

        public static void Show(string playerName, string myConnectionId, string playerId, float duration = 2.8f)
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            Hide();

            bool iDidIt = !string.IsNullOrEmpty(myConnectionId)
                          && !string.IsNullOrEmpty(playerId)
                          && myConnectionId == playerId;

            string who = string.IsNullOrEmpty(playerName) ? "Alguien" : playerName;
            string subtitle = iDidIt ? "¡Pepineaste!" : $"{who} te pepineó";

            var root = new GameObject(RootName, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            root.transform.SetParent(canvas.transform, false);
            root.transform.SetAsLastSibling();

            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var bg = root.GetComponent<Image>();
            bg.color = PanelBg;
            bg.raycastTarget = false;

            var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(root.transform, false);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(520f, 200f);
            cardRect.anchoredPosition = Vector2.zero;
            var cardImg = card.GetComponent<Image>();
            cardImg.color = new Color(0.1f, 0.14f, 0.12f, 0.95f);
            cardImg.raycastTarget = false;

            // Accent bar
            var bar = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(card.transform, false);
            var barRect = bar.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 1f);
            barRect.anchorMax = new Vector2(1f, 1f);
            barRect.pivot = new Vector2(0.5f, 1f);
            barRect.sizeDelta = new Vector2(0f, 6f);
            barRect.anchoredPosition = Vector2.zero;
            bar.GetComponent<Image>().color = Accent;
            bar.GetComponent<Image>().raycastTarget = false;

            var titleGo = CreateTmp(card.transform, "Title", "PEPINEADO", 56f, Gold, FontStyles.Bold);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.55f);
            titleRect.anchorMax = new Vector2(0.5f, 0.55f);
            titleRect.sizeDelta = new Vector2(480f, 70f);
            titleRect.anchoredPosition = Vector2.zero;

            var subGo = CreateTmp(card.transform, "Subtitle", subtitle, 28f, Cream, FontStyles.Normal);
            var subRect = subGo.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.5f, 0.28f);
            subRect.anchorMax = new Vector2(0.5f, 0.28f);
            subRect.sizeDelta = new Vector2(460f, 48f);
            subRect.anchoredPosition = Vector2.zero;

            var cg = root.GetComponent<CanvasGroup>();
            cg.alpha = 0f;
            cardRect.localScale = Vector3.one * 0.82f;

            LeanTween.alphaCanvas(cg, 1f, 0.22f).setEaseOutQuad();
            LeanTween.scale(card, Vector3.one, 0.35f).setEaseOutBack();

            LeanTween.delayedCall(duration, () =>
            {
                if (root == null) return;
                LeanTween.alphaCanvas(cg, 0f, 0.25f).setOnComplete(() =>
                {
                    if (root != null) Object.Destroy(root);
                });
            });
        }

        public static void Hide()
        {
            var existing = GameObject.Find(RootName);
            if (existing != null)
                Object.Destroy(existing);
        }

        private static GameObject CreateTmp(
            Transform parent,
            string name,
            string text,
            float size,
            Color color,
            FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return go;
        }
    }
}
