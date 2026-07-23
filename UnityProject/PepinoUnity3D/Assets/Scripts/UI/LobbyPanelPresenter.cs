using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PepinoGame.UI
{
    /// <summary>
    /// Left docked lobby sidebar (mockup): title, room code, players, decks, start.
    /// </summary>
    public static class LobbyPanelPresenter
    {
        private static readonly Color PanelBg = new Color(0.07f, 0.08f, 0.1f, 0.94f);
        private static readonly Color Cream = new Color(0.98f, 0.96f, 0.9f, 1f);
        private static readonly Color Muted = new Color(0.7f, 0.74f, 0.78f, 1f);
        private static readonly Color PlayGreen = new Color(0.18f, 0.72f, 0.36f, 1f);
        private static readonly Color DisabledGrey = new Color(0.32f, 0.34f, 0.38f, 1f);
        private static readonly Color Accent = new Color(0.35f, 0.85f, 0.45f, 1f);

        public const float SidebarWidth = 380f;

        public static void Apply(
            GameObject panelRoot,
            TextMeshProUGUI titleText,
            TextMeshProUGUI statusText,
            TextMeshProUGUI roomCodeText,
            TextMeshProUGUI playersInfoText,
            TextMeshProUGUI modeInfoText,
            Button copyRoomButton,
            Button deck1,
            Button deck2,
            Button deck3,
            Button startGame,
            TextMeshProUGUI waitingHintText)
        {
            if (panelRoot == null) return;

            EnsurePanelChrome(panelRoot);
            PlaceTitle(titleText);
            PlaceRoomCode(roomCodeText, copyRoomButton);
            PlaceStatus(statusText);
            PlacePlayers(playersInfoText);
            PlaceModeInfo(modeInfoText);
            PlaceDeckColumn(deck1, deck2, deck3);
            PlaceHint(waitingHintText);
            PlaceStart(startGame);
        }

        public static void StyleStartButton(Button button, bool canStart)
        {
            if (button == null) return;

            button.interactable = canStart;

            var img = button.GetComponent<Image>();
            if (img != null)
                img.color = canStart ? PlayGreen : DisabledGrey;

            var tmp = button.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = "INICIAR PARTIDA";
                tmp.fontSize = 22;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = Cream;
                tmp.alignment = TextAlignmentOptions.Center;
            }
        }

        public static void StyleCopyButton(Button button)
        {
            if (button == null) return;
            var img = button.GetComponent<Image>();
            if (img != null)
                img.color = new Color(0.2f, 0.22f, 0.26f, 1f);

            var tmp = button.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = "COPIAR";
                tmp.fontSize = 14;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = Cream;
                tmp.alignment = TextAlignmentOptions.Center;
            }
        }

        private static void EnsurePanelChrome(GameObject panelRoot)
        {
            var rect = panelRoot.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.offsetMin = new Vector2(12f, 12f);
                rect.offsetMax = new Vector2(12f + SidebarWidth, -12f);
            }

            var img = panelRoot.GetComponent<Image>();
            if (img == null)
                img = panelRoot.AddComponent<Image>();
            img.color = PanelBg;
            img.raycastTarget = true;
        }

        private static void PlaceTitle(TextMeshProUGUI text)
        {
            if (text == null) return;
            StretchTop(text.rectTransform, 16f, 36f);
            text.fontSize = 28;
            text.fontStyle = FontStyles.Bold;
            text.color = Cream;
            text.alignment = TextAlignmentOptions.Left;
            text.margin = new Vector4(20f, 0f, 20f, 0f);
            text.enableWordWrapping = false;
        }

        private static void PlaceRoomCode(TextMeshProUGUI text, Button copy)
        {
            if (text != null)
            {
                StretchTop(text.rectTransform, 56f, 28f);
                text.fontSize = 16;
                text.color = Muted;
                text.alignment = TextAlignmentOptions.Left;
                text.margin = new Vector4(20f, 0f, 110f, 0f);
                text.enableWordWrapping = false;
            }

            if (copy != null)
            {
                var rect = copy.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(1f, 1f);
                    rect.anchorMax = new Vector2(1f, 1f);
                    rect.pivot = new Vector2(1f, 1f);
                    rect.anchoredPosition = new Vector2(-16f, -52f);
                    rect.sizeDelta = new Vector2(88f, 32f);
                }

                StyleCopyButton(copy);
            }
        }

        private static void PlaceStatus(TextMeshProUGUI text)
        {
            if (text == null) return;
            StretchTop(text.rectTransform, 92f, 28f);
            text.fontSize = 16;
            text.fontStyle = FontStyles.Bold;
            text.color = Accent;
            text.alignment = TextAlignmentOptions.Left;
            text.margin = new Vector4(20f, 0f, 20f, 0f);
        }

        private static void PlacePlayers(TextMeshProUGUI text)
        {
            if (text == null) return;
            StretchTop(text.rectTransform, 128f, 200f);
            text.fontSize = 17;
            text.color = Cream;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.enableWordWrapping = true;
            text.richText = true;
            text.margin = new Vector4(20f, 0f, 20f, 0f);
        }

        private static void PlaceModeInfo(TextMeshProUGUI text)
        {
            if (text == null) return;
            // Below player cards (~128 + ~220)
            StretchTop(text.rectTransform, 360f, 32f);
            text.fontSize = 14;
            text.color = Muted;
            text.alignment = TextAlignmentOptions.Left;
            text.margin = new Vector4(20f, 0f, 20f, 0f);
            text.enableWordWrapping = true;
        }

        private static void PlaceDeckColumn(Button a, Button b, Button c)
        {
            PlaceDeckButton(a, 400f);
            PlaceDeckButton(b, 456f);
            PlaceDeckButton(c, 512f);
        }

        private static void PlaceDeckButton(Button button, float top)
        {
            if (button == null) return;
            var rect = button.GetComponent<RectTransform>();
            if (rect == null) return;

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -top);
            rect.sizeDelta = new Vector2(-40f, 48f);

            var tmp = button.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.fontSize = 16;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.enableWordWrapping = false;
            }
        }

        private static void PlaceHint(TextMeshProUGUI text)
        {
            if (text == null) return;
            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 96f);
            rect.sizeDelta = new Vector2(-32f, 40f);
            text.fontSize = 14;
            text.color = new Color(0.9f, 0.78f, 0.35f, 1f);
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = true;
        }

        private static void PlaceStart(Button button)
        {
            if (button == null) return;
            var rect = button.GetComponent<RectTransform>();
            if (rect == null) return;

            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 24f);
            rect.sizeDelta = new Vector2(-40f, 58f);
        }

        private static void StretchTop(RectTransform rect, float topOffset, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -topOffset);
            rect.sizeDelta = new Vector2(0f, height);
        }
    }
}
