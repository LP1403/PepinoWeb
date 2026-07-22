using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PepinoGame.UI
{
    /// <summary>
    /// Runtime polish so the default Unity HUD feels more like a card game (UNO-ish).
    /// Does not require rewiring the scene — restyles existing refs.
    /// </summary>
    public static class GameHudPresenter
    {
        private static readonly Color FeltGreen = new Color(0.12f, 0.45f, 0.28f, 0.92f);
        private static readonly Color PlayGreen = new Color(0.18f, 0.72f, 0.32f, 1f);
        private static readonly Color PassDark = new Color(0.22f, 0.25f, 0.3f, 1f);
        private static readonly Color Cream = new Color(0.98f, 0.96f, 0.9f, 1f);
        private static readonly Color TurnGold = new Color(1f, 0.85f, 0.2f, 1f);

        public static void Apply(
            TextMeshProUGUI roomInfoText,
            TextMeshProUGUI turnInfoText,
            TextMeshProUGUI notificationText,
            Button playCardsButton,
            Button passTurnButton)
        {
            StyleRoomChip(roomInfoText);
            StyleTurnBanner(turnInfoText);
            StyleNotification(notificationText);
            StyleActionButton(playCardsButton, "JUGAR", PlayGreen, Cream);
            StyleActionButton(passTurnButton, "PASAR", PassDark, Cream);
            LayoutActionButtons(playCardsButton, passTurnButton);
        }

        private static void StyleRoomChip(TextMeshProUGUI text)
        {
            if (text == null) return;

            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24f, -20f);
            rect.sizeDelta = new Vector2(320f, 56f);

            text.fontSize = 22;
            text.fontStyle = FontStyles.Bold;
            text.color = Cream;
            text.alignment = TextAlignmentOptions.Left;
            text.enableWordWrapping = false;

            EnsureBackdrop(text.gameObject, FeltGreen, new Vector2(16f, 10f));
        }

        private static void StyleTurnBanner(TextMeshProUGUI text)
        {
            if (text == null) return;

            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -28f);
            rect.sizeDelta = new Vector2(420f, 64f);

            text.fontSize = 36;
            text.fontStyle = FontStyles.Bold;
            text.color = TurnGold;
            text.alignment = TextAlignmentOptions.Center;
            text.outlineWidth = 0.25f;
            text.outlineColor = new Color(0f, 0f, 0f, 0.85f);

            EnsureBackdrop(text.gameObject, new Color(0f, 0f, 0f, 0.45f), new Vector2(24f, 12f));
        }

        private static void StyleNotification(TextMeshProUGUI text)
        {
            if (text == null) return;

            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.55f);
            rect.anchorMax = new Vector2(0.5f, 0.55f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(520f, 80f);

            text.fontSize = 28;
            text.fontStyle = FontStyles.Bold;
            text.color = Cream;
            text.alignment = TextAlignmentOptions.Center;
        }

        private static void StyleActionButton(Button button, string label, Color bg, Color fg)
        {
            if (button == null) return;

            var img = button.GetComponent<Image>();
            if (img != null)
            {
                img.color = bg;
                img.raycastTarget = true;
            }

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.55f);
            button.colors = colors;

            var tmp = button.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                if (!string.IsNullOrEmpty(label))
                    tmp.text = label;
                tmp.fontSize = 26;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = fg;
                tmp.alignment = TextAlignmentOptions.Center;
            }
        }

        private static void LayoutActionButtons(Button play, Button pass)
        {
            // Thumb-friendly cluster bottom-right (UNO "CALL UNO" zone)
            PlaceButton(pass, new Vector2(-200f, 120f), new Vector2(160f, 64f));
            PlaceButton(play, new Vector2(-28f, 120f), new Vector2(180f, 72f));
        }

        private static void PlaceButton(Button button, Vector2 anchoredPos, Vector2 size)
        {
            if (button == null) return;
            var rect = button.GetComponent<RectTransform>();
            if (rect == null) return;

            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
        }

        private static void EnsureBackdrop(GameObject target, Color color, Vector2 padding)
        {
            const string childName = "HudBackdrop";
            var existing = target.transform.Find(childName);
            Image img;
            if (existing == null)
            {
                var go = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(target.transform, false);
                go.transform.SetAsFirstSibling();
                img = go.GetComponent<Image>();
            }
            else
            {
                img = existing.GetComponent<Image>();
            }

            img.color = color;
            img.raycastTarget = false;

            var rect = img.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-padding.x, -padding.y);
            rect.offsetMax = new Vector2(padding.x, padding.y);
        }
    }
}
