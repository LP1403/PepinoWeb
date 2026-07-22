using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PepinoGame.UI
{
    /// <summary>
    /// Lightweight HUD polish — no heavy colored boxes, outline text + solid action buttons.
    /// </summary>
    public static class GameHudPresenter
    {
        private static readonly Color PlayGreen = new Color(0.16f, 0.7f, 0.34f, 1f);
        private static readonly Color PassDark = new Color(0.2f, 0.22f, 0.26f, 1f);
        private static readonly Color Cream = new Color(0.98f, 0.96f, 0.9f, 1f);
        private static readonly Color TurnGold = new Color(1f, 0.88f, 0.25f, 1f);

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

            RemoveBackdrop(text.gameObject);

            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(18f, -14f);
            rect.sizeDelta = new Vector2(360f, 40f);

            text.fontSize = 18;
            text.fontStyle = FontStyles.Bold;
            text.color = new Color(1f, 1f, 1f, 0.9f);
            text.alignment = TextAlignmentOptions.Left;
            text.enableWordWrapping = false;
            text.outlineWidth = 0.2f;
            text.outlineColor = new Color(0f, 0f, 0f, 0.75f);
        }

        private static void StyleTurnBanner(TextMeshProUGUI text)
        {
            if (text == null) return;

            RemoveBackdrop(text.gameObject);

            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -18f);
            rect.sizeDelta = new Vector2(480f, 56f);

            text.fontSize = 34;
            text.fontStyle = FontStyles.Bold;
            text.color = TurnGold;
            text.alignment = TextAlignmentOptions.Center;
            text.outlineWidth = 0.28f;
            text.outlineColor = new Color(0f, 0f, 0f, 0.9f);
        }

        private static void StyleNotification(TextMeshProUGUI text)
        {
            if (text == null) return;

            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.62f);
            rect.anchorMax = new Vector2(0.5f, 0.62f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(520f, 64f);

            text.fontSize = 24;
            text.fontStyle = FontStyles.Bold;
            text.color = Cream;
            text.alignment = TextAlignmentOptions.Center;
            text.outlineWidth = 0.22f;
            text.outlineColor = Color.black;
        }

        private static void StyleActionButton(Button button, string label, Color bg, Color fg)
        {
            if (button == null) return;

            var img = button.GetComponent<Image>();
            if (img != null)
                img.color = bg;

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.45f);
            button.colors = colors;

            var tmp = button.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = label;
                tmp.fontSize = 26;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = fg;
                tmp.alignment = TextAlignmentOptions.Center;
            }
        }

        private static void LayoutActionButtons(Button play, Button pass)
        {
            PlaceButton(pass, new Vector2(-24f, 24f), new Vector2(140f, 54f));
            PlaceButton(play, new Vector2(-24f, 88f), new Vector2(160f, 60f));
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

        private static void RemoveBackdrop(GameObject target)
        {
            var existing = target.transform.Find("HudBackdrop");
            if (existing != null)
                Object.Destroy(existing.gameObject);
        }
    }
}
