using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PepinoGame.Models;

namespace PepinoGame.UI
{
    /// <summary>
    /// In-game HUD: left match sidebar, turn banner, JUGAR/PASAR bottom-center.
    /// </summary>
    public static class GameHudPresenter
    {
        private static readonly Color PlayGreen = new Color(0.16f, 0.7f, 0.34f, 1f);
        private static readonly Color PassLight = new Color(0.92f, 0.93f, 0.95f, 1f);
        private static readonly Color Cream = new Color(0.98f, 0.96f, 0.9f, 1f);
        private static readonly Color TurnGold = new Color(1f, 0.88f, 0.25f, 1f);
        private static readonly Color AccentGreen = new Color(0.45f, 0.92f, 0.55f, 1f);
        private static readonly Color PanelBg = new Color(0.07f, 0.08f, 0.1f, 0.9f);
        private static readonly Color DarkText = new Color(0.12f, 0.14f, 0.16f, 1f);

        public static void Apply(
            TextMeshProUGUI roomInfoText,
            TextMeshProUGUI turnInfoText,
            TextMeshProUGUI notificationText,
            TextMeshProUGUI playersInfoText,
            Button playCardsButton,
            Button passTurnButton,
            bool gameStarted)
        {
            StyleMatchSidebar(roomInfoText, playersInfoText, gameStarted);
            StyleTurnBanner(turnInfoText);
            StyleNotification(notificationText);
            StyleActionButton(playCardsButton, "JUGAR CARTA", PlayGreen, Cream);
            StyleActionButton(passTurnButton, "PASAR", PassLight, DarkText);
            LayoutActionButtons(playCardsButton, passTurnButton);
        }

        public static string BuildPlayersSidebar(GameState state, string myConnectionId)
        {
            if (state?.players == null) return "";

            var sb = new StringBuilder();
            sb.AppendLine("<b>Jugadores</b>");

            for (int i = 0; i < state.players.Count; i++)
            {
                var p = state.players[i];
                if (p == null) continue;
                bool isMe = !string.IsNullOrEmpty(myConnectionId) && p.connectionId == myConnectionId;
                string name = isMe ? "TÚ" : (string.IsNullOrEmpty(p.name) ? $"J{i + 1}" : p.name);
                string mark = p.isCurrentTurn ? "> " : "";
                string won = p.hasWon ? " OK" : "";
                sb.AppendLine($"{mark}{name}  ·  {p.cardCount}{won}");
            }

            return sb.ToString().TrimEnd();
        }

        private static void StyleMatchSidebar(
            TextMeshProUGUI roomInfoText,
            TextMeshProUGUI playersInfoText,
            bool gameStarted)
        {
            if (roomInfoText != null)
            {
                roomInfoText.gameObject.SetActive(gameStarted);
                if (gameStarted)
                {
                    EnsureSidebarChrome(roomInfoText.gameObject);
                    var rect = roomInfoText.rectTransform;
                    rect.anchorMin = new Vector2(0f, 1f);
                    rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0f, 1f);
                    rect.anchoredPosition = new Vector2(24f, -28f);
                    rect.sizeDelta = new Vector2(340f, 48f);
                    roomInfoText.fontSize = 18;
                    roomInfoText.fontStyle = FontStyles.Bold;
                    roomInfoText.color = Cream;
                    roomInfoText.alignment = TextAlignmentOptions.Left;
                    roomInfoText.margin = new Vector4(16f, 8f, 16f, 0f);
                    roomInfoText.richText = true;
                }
            }

            if (playersInfoText != null)
            {
                playersInfoText.gameObject.SetActive(gameStarted);
                if (gameStarted)
                {
                    EnsureSidebarChrome(playersInfoText.gameObject);
                    var rect = playersInfoText.rectTransform;
                    rect.anchorMin = new Vector2(0f, 1f);
                    rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0f, 1f);
                    rect.anchoredPosition = new Vector2(24f, -84f);
                    rect.sizeDelta = new Vector2(340f, 280f);
                    playersInfoText.fontSize = 17;
                    playersInfoText.color = Cream;
                    playersInfoText.alignment = TextAlignmentOptions.TopLeft;
                    playersInfoText.richText = true;
                    playersInfoText.enableWordWrapping = true;
                    playersInfoText.margin = new Vector4(16f, 12f, 16f, 12f);
                }
            }
        }

        private static void EnsureSidebarChrome(GameObject target)
        {
            // TMP and Image can't share the same GameObject (one Graphic only).
            // Put the panel background on a child behind the text.
            var existing = target.transform.Find("SidebarChrome");
            Image img;
            if (existing == null)
            {
                var go = new GameObject("SidebarChrome", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(target.transform, false);
                go.transform.SetAsFirstSibling();
                img = go.GetComponent<Image>();
            }
            else
            {
                img = existing.GetComponent<Image>();
                if (img == null)
                    img = existing.gameObject.AddComponent<Image>();
            }

            var rect = img.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            img.color = PanelBg;
            img.raycastTarget = false;
        }

        private static void StyleTurnBanner(TextMeshProUGUI text)
        {
            if (text == null) return;

            var rect = text.rectTransform;
            // Top strip — never cover the discard pile / table center
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -18f);
            rect.sizeDelta = new Vector2(420f, 44f);

            text.fontSize = 26;
            text.fontStyle = FontStyles.Bold;
            text.color = TurnGold;
            text.alignment = TextAlignmentOptions.Center;
            text.outlineWidth = 0.25f;
            text.outlineColor = new Color(0f, 0f, 0f, 0.95f);

            EnsureTurnBackdrop(text.gameObject);
        }

        private static void EnsureTurnBackdrop(GameObject target)
        {
            var existing = target.transform.Find("TurnBackdrop");
            Image img;
            if (existing == null)
            {
                var go = new GameObject("TurnBackdrop", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(target.transform, false);
                go.transform.SetAsFirstSibling();
                img = go.GetComponent<Image>();
            }
            else
            {
                img = existing.GetComponent<Image>();
            }

            var rect = img.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-16f, -4f);
            rect.offsetMax = new Vector2(16f, 4f);
            img.color = new Color(0f, 0f, 0f, 0.62f);
            img.raycastTarget = false;
        }

        public static void SetTurnBannerState(
            TextMeshProUGUI text,
            bool isMyTurn,
            string message,
            bool freePlay = false)
        {
            if (text == null) return;
            text.gameObject.SetActive(true);
            text.text = message;
            text.richText = true;
            text.enableWordWrapping = true;

            if (freePlay)
            {
                text.color = AccentGreen;
                text.fontSize = 24f;
                var rect = text.rectTransform;
                rect.sizeDelta = new Vector2(460f, 64f);
            }
            else
            {
                text.color = isMyTurn ? TurnGold : Cream;
                text.fontSize = isMyTurn ? 26f : 22f;
                var rect = text.rectTransform;
                rect.sizeDelta = new Vector2(420f, 44f);
            }

            var backdrop = text.transform.Find("TurnBackdrop");
            if (backdrop != null)
            {
                var img = backdrop.GetComponent<Image>();
                if (img != null)
                {
                    if (freePlay)
                        img.color = new Color(0.05f, 0.18f, 0.1f, 0.78f);
                    else if (isMyTurn)
                        img.color = new Color(0.12f, 0.1f, 0f, 0.72f);
                    else
                        img.color = new Color(0f, 0f, 0f, 0.55f);
                }
            }
        }

        private static void StyleNotification(TextMeshProUGUI text)
        {
            if (text == null) return;

            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -68f);
            rect.sizeDelta = new Vector2(480f, 40f);

            text.fontSize = 18;
            text.fontStyle = FontStyles.Bold;
            text.color = Cream;
            text.alignment = TextAlignmentOptions.Center;
            text.outlineWidth = 0.2f;
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
            colors.highlightedColor = new Color(1.05f, 1.05f, 1.05f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.45f);
            button.colors = colors;

            var tmp = button.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = label;
                tmp.fontSize = 22;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = fg;
                tmp.alignment = TextAlignmentOptions.Center;
            }
        }

        private static void LayoutActionButtons(Button play, Button pass)
        {
            // Above the raised hand band, below table mid — don't cover discard
            PlaceCentered(play, new Vector2(-120f, 290f), new Vector2(200f, 52f));
            PlaceCentered(pass, new Vector2(120f, 290f), new Vector2(170f, 52f));
        }

        private static void PlaceCentered(Button button, Vector2 anchoredPos, Vector2 size)
        {
            if (button == null) return;
            var rect = button.GetComponent<RectTransform>();
            if (rect == null) return;

            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
        }
    }
}
