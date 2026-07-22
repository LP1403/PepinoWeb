using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PepinoGame.UI
{
    /// <summary>
    /// Ordena el panel de lobby: título → jugadores → mazos → iniciar.
    /// </summary>
    public static class LobbyPanelPresenter
    {
        private static readonly Color PanelBg = new Color(0.08f, 0.1f, 0.14f, 0.92f);
        private static readonly Color Cream = new Color(0.98f, 0.96f, 0.9f, 1f);
        private static readonly Color Muted = new Color(0.75f, 0.78f, 0.82f, 1f);
        private static readonly Color PlayGreen = new Color(0.16f, 0.7f, 0.34f, 1f);
        private static readonly Color DisabledGrey = new Color(0.35f, 0.37f, 0.4f, 1f);

        public static void Apply(
            GameObject panelRoot,
            TextMeshProUGUI titleText,
            TextMeshProUGUI playersInfoText,
            TextMeshProUGUI modeInfoText,
            Button deck1,
            Button deck2,
            Button deck3,
            Button startGame)
        {
            if (panelRoot == null) return;

            EnsurePanelChrome(panelRoot);
            PlaceTitle(titleText);
            PlacePlayers(playersInfoText);
            PlaceModeInfo(modeInfoText);
            PlaceDeckRow(deck1, deck2, deck3);
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
                tmp.text = canStart ? "Iniciar partida" : "Iniciar partida";
                tmp.fontSize = 28;
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
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(520f, 520f);
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
            var rect = text.rectTransform;
            StretchTop(rect, 18f, 48f);
            text.fontSize = 30;
            text.fontStyle = FontStyles.Bold;
            text.color = Cream;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
        }

        private static void PlacePlayers(TextMeshProUGUI text)
        {
            if (text == null) return;
            var rect = text.rectTransform;
            StretchTop(rect, 72f, 150f);
            text.fontSize = 20;
            text.fontStyle = FontStyles.Normal;
            text.color = Cream;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.enableWordWrapping = true;
            text.richText = true;
            text.margin = new Vector4(28f, 0f, 28f, 0f);
        }

        private static void PlaceModeInfo(TextMeshProUGUI text)
        {
            if (text == null) return;
            var rect = text.rectTransform;
            StretchTop(rect, 230f, 44f);
            text.fontSize = 17;
            text.color = Muted;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
            text.margin = Vector4.zero;
        }

        private static void PlaceDeckRow(Button a, Button b, Button c)
        {
            PlaceDeckButton(a, new Vector2(-168f, 40f));
            PlaceDeckButton(b, new Vector2(0f, 40f));
            PlaceDeckButton(c, new Vector2(168f, 40f));
        }

        private static void PlaceDeckButton(Button button, Vector2 anchoredPos)
        {
            if (button == null) return;
            var rect = button.GetComponent<RectTransform>();
            if (rect == null) return;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(150f, 72f);

            var tmp = button.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.fontSize = 16;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.enableWordWrapping = true;
            }
        }

        private static void PlaceStart(Button button)
        {
            if (button == null) return;
            var rect = button.GetComponent<RectTransform>();
            if (rect == null) return;

            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 28f);
            rect.sizeDelta = new Vector2(280f, 64f);
        }

        private static void StretchTop(RectTransform rect, float topOffset, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -topOffset);
            rect.sizeDelta = new Vector2(-24f, height);
        }
    }
}
