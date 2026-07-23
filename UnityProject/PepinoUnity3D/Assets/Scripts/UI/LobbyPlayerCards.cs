using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PepinoGame.Models;

namespace PepinoGame.UI
{
    /// <summary>
    /// Mockup-style lobby player rows (avatar + name + seat color chip).
    /// </summary>
    public static class LobbyPlayerCards
    {
        private static readonly Color RowBg = new Color(0.14f, 0.16f, 0.2f, 0.95f);
        private static readonly Color Cream = new Color(0.98f, 0.96f, 0.9f, 1f);
        private static readonly Color Muted = new Color(0.7f, 0.74f, 0.78f, 1f);

        private static readonly Color[] SeatColors =
        {
            new Color(0.15f, 0.75f, 0.65f),
            new Color(0.25f, 0.45f, 0.95f),
            new Color(0.9f, 0.75f, 0.2f),
            new Color(0.65f, 0.3f, 0.85f),
            new Color(0.9f, 0.35f, 0.35f),
            new Color(0.35f, 0.75f, 0.4f),
            new Color(0.95f, 0.55f, 0.2f),
            new Color(0.5f, 0.55f, 0.7f),
        };

        public static RectTransform EnsureContainer(Transform parent)
        {
            var existing = parent.Find("LobbyPlayerList");
            if (existing != null)
                return existing.GetComponent<RectTransform>();

            var go = new GameObject("LobbyPlayerList", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            go.transform.SetParent(parent, false);

            var vlg = go.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.padding = new RectOffset(16, 16, 4, 4);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;

            var fitter = go.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            return go.GetComponent<RectTransform>();
        }

        public static void Place(RectTransform list)
        {
            if (list == null) return;
            list.anchorMin = new Vector2(0f, 1f);
            list.anchorMax = new Vector2(1f, 1f);
            list.pivot = new Vector2(0.5f, 1f);
            list.anchoredPosition = new Vector2(0f, -128f);
            list.sizeDelta = new Vector2(0f, 220f);
        }

        public static void Rebuild(RectTransform list, List<Player> players, string myConnectionId)
        {
            if (list == null) return;

            for (int i = list.childCount - 1; i >= 0; i--)
                Object.Destroy(list.GetChild(i).gameObject);

            if (players == null || players.Count == 0)
            {
                CreateEmptyHint(list);
                return;
            }

            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                bool isMe = !string.IsNullOrEmpty(myConnectionId) && p.connectionId == myConnectionId;
                bool isHost = i == 0;
                string display = isMe ? "TÚ" : (string.IsNullOrEmpty(p.name) ? $"Jugador {i + 1}" : p.name);
                if (isHost) display += " (Anfitrión)";

                CreateRow(list, display, i, SeatColors[i % SeatColors.Length]);
            }
        }

        private static void CreateEmptyHint(RectTransform list)
        {
            var go = new GameObject("Empty", typeof(RectTransform));
            go.transform.SetParent(list, false);
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 40f;
            le.preferredHeight = 40f;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "Nadie en la sala todavía";
            tmp.fontSize = 15;
            tmp.color = Muted;
            tmp.alignment = TextAlignmentOptions.Center;
        }

        private static void CreateRow(RectTransform list, string displayName, int index, Color seatColor)
        {
            var row = new GameObject($"Player_{index}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            row.transform.SetParent(list, false);

            var le = row.GetComponent<LayoutElement>();
            le.minHeight = 52f;
            le.preferredHeight = 52f;

            var bg = row.GetComponent<Image>();
            bg.color = RowBg;
            bg.raycastTarget = false;

            // Avatar circle
            var avatar = new GameObject("Avatar", typeof(RectTransform), typeof(Image));
            avatar.transform.SetParent(row.transform, false);
            var avRect = avatar.GetComponent<RectTransform>();
            avRect.anchorMin = new Vector2(0f, 0.5f);
            avRect.anchorMax = new Vector2(0f, 0.5f);
            avRect.pivot = new Vector2(0f, 0.5f);
            avRect.anchoredPosition = new Vector2(10f, 0f);
            avRect.sizeDelta = new Vector2(36f, 36f);
            var avImg = avatar.GetComponent<Image>();
            avImg.color = Color.Lerp(seatColor, Color.white, 0.35f);
            avImg.raycastTarget = false;

            var initial = new GameObject("Initial", typeof(RectTransform));
            initial.transform.SetParent(avatar.transform, false);
            StretchFull(initial.GetComponent<RectTransform>());
            var initialTmp = initial.AddComponent<TextMeshProUGUI>();
            initialTmp.text = string.IsNullOrEmpty(displayName) ? "?" : displayName.Substring(0, 1).ToUpperInvariant();
            if (displayName.StartsWith("TÚ")) initialTmp.text = "T";
            initialTmp.fontSize = 16;
            initialTmp.fontStyle = FontStyles.Bold;
            initialTmp.color = Cream;
            initialTmp.alignment = TextAlignmentOptions.Center;

            // Name
            var nameGo = new GameObject("Name", typeof(RectTransform));
            nameGo.transform.SetParent(row.transform, false);
            var nameRect = nameGo.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.offsetMin = new Vector2(56f, 6f);
            nameRect.offsetMax = new Vector2(-48f, -6f);
            var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
            nameTmp.text = displayName;
            nameTmp.fontSize = 16;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.color = Cream;
            nameTmp.alignment = TextAlignmentOptions.Left;
            nameTmp.enableWordWrapping = false;
            nameTmp.overflowMode = TextOverflowModes.Ellipsis;

            // Seat color chip (right)
            var chip = new GameObject("Chip", typeof(RectTransform), typeof(Image));
            chip.transform.SetParent(row.transform, false);
            var chipRect = chip.GetComponent<RectTransform>();
            chipRect.anchorMin = new Vector2(1f, 0.5f);
            chipRect.anchorMax = new Vector2(1f, 0.5f);
            chipRect.pivot = new Vector2(1f, 0.5f);
            chipRect.anchoredPosition = new Vector2(-12f, 0f);
            chipRect.sizeDelta = new Vector2(28f, 28f);
            var chipImg = chip.GetComponent<Image>();
            chipImg.color = seatColor;
            chipImg.raycastTarget = false;

            var chipLabel = new GameObject("Num", typeof(RectTransform));
            chipLabel.transform.SetParent(chip.transform, false);
            StretchFull(chipLabel.GetComponent<RectTransform>());
            var chipTmp = chipLabel.AddComponent<TextMeshProUGUI>();
            chipTmp.text = (index + 1).ToString();
            chipTmp.fontSize = 14;
            chipTmp.fontStyle = FontStyles.Bold;
            chipTmp.color = Cream;
            chipTmp.alignment = TextAlignmentOptions.Center;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
