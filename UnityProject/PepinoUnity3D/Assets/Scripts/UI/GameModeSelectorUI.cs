using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PepinoGame.Managers;
using PepinoGame.Models;

namespace PepinoGame.UI
{
    /// <summary>
    /// Lobby de sala antes de iniciar: lista de jugadores, mazos e Iniciar (≥2 jugadores).
    /// </summary>
    public class GameModeSelectorUI : MonoBehaviour
    {
        private const int MinPlayersToStart = 2;

        [Header("UI References")]
        [SerializeField] private GameObject selectorPanel;
        [SerializeField] private Button deck1Button;
        [SerializeField] private Button deck2Button;
        [SerializeField] private Button deck3Button;
        [SerializeField] private Button startGameButton;
        [SerializeField] private TextMeshProUGUI modeInfoText;
        [SerializeField] private TextMeshProUGUI playersInfoText;
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Colors")]
        [SerializeField] private Color selectedColor = new Color(0.25f, 0.85f, 0.45f, 1f);
        [SerializeField] private Color normalColor = Color.white;

        private int selectedDeckCount;
        private bool isRoomCreator;
        private bool layoutApplied;
        private TextMeshProUGUI waitingHintText;

        private void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;

                if (GameManager.Instance.CurrentGameState != null)
                    OnGameStateChanged(GameManager.Instance.CurrentGameState);
            }

            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnGameStarted -= OnGameStarted;
                NetworkManager.Instance.OnGameStarted += OnGameStarted;
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;

            if (NetworkManager.Instance != null)
                NetworkManager.Instance.OnGameStarted -= OnGameStarted;
        }

        private void Start()
        {
            EnsureTitleText();
            EnsureWaitingHint();
            ApplyLayout();

            if (deck1Button != null)
                deck1Button.onClick.AddListener(() => SelectDeckCount(1));
            if (deck2Button != null)
                deck2Button.onClick.AddListener(() => SelectDeckCount(2));
            if (deck3Button != null)
                deck3Button.onClick.AddListener(() => SelectDeckCount(3));

            if (startGameButton != null)
            {
                startGameButton.onClick.AddListener(OnStartGameClicked);
                LobbyPanelPresenter.StyleStartButton(startGameButton, false);
            }

            UpdateUI(GameManager.Instance?.CurrentGameState);
        }

        private void EnsureTitleText()
        {
            if (titleText != null) return;

            var existing = transform.Find("LobbyTitle");
            if (existing != null)
            {
                titleText = existing.GetComponent<TextMeshProUGUI>();
                if (titleText != null) return;
            }

            var parent = selectorPanel != null ? selectorPanel.transform : transform;
            var go = new GameObject("LobbyTitle", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.transform.SetAsFirstSibling();
            titleText = go.AddComponent<TextMeshProUGUI>();
            titleText.text = "Lobby";
        }

        private void EnsureWaitingHint()
        {
            if (waitingHintText != null) return;

            var parent = selectorPanel != null ? selectorPanel.transform : transform;
            var existing = parent.Find("WaitingHint");
            if (existing != null)
            {
                waitingHintText = existing.GetComponent<TextMeshProUGUI>();
                if (waitingHintText != null) return;
            }

            var go = new GameObject("WaitingHint", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            waitingHintText = go.AddComponent<TextMeshProUGUI>();
            waitingHintText.fontSize = 16;
            waitingHintText.color = new Color(0.85f, 0.75f, 0.35f, 1f);
            waitingHintText.alignment = TextAlignmentOptions.Center;

            var rect = waitingHintText.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 100f);
            rect.sizeDelta = new Vector2(-32f, 36f);
        }

        private void ApplyLayout()
        {
            if (layoutApplied) return;
            var root = selectorPanel != null ? selectorPanel : gameObject;
            LobbyPanelPresenter.Apply(
                root,
                titleText,
                playersInfoText,
                modeInfoText,
                deck1Button,
                deck2Button,
                deck3Button,
                startGameButton);
            layoutApplied = true;
        }

        private async void SelectDeckCount(int deckCount)
        {
            if (GameManager.Instance?.CurrentGameState != null)
                isRoomCreator = GameManager.Instance.CurrentGameState.isRoomCreator;

            if (!isRoomCreator)
            {
                Debug.Log("[Lobby] Solo el anfitrión puede elegir el modo");
                return;
            }

            string roomId = GameManager.Instance?.CurrentRoomId;
            if (string.IsNullOrWhiteSpace(roomId))
            {
                Debug.LogError("[Lobby] CurrentRoomId vacío — volvé a unirte a la sala");
                return;
            }

            selectedDeckCount = deckCount;
            UpdateDeckButtonsVisuals();
            UpdateModeInfoFromSelection();
            RefreshStartButton(GameManager.Instance?.CurrentGameState);

            try
            {
                await NetworkManager.Instance.SelectGameMode(roomId, deckCount);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Lobby] Error al seleccionar modo: {ex.Message}");
            }

            RefreshStartButton(GameManager.Instance?.CurrentGameState);
        }

        private async void OnStartGameClicked()
        {
            if (!isRoomCreator) return;

            var state = GameManager.Instance?.CurrentGameState;
            int playerCount = state?.players?.Count ?? 0;

            if (selectedDeckCount == 0)
            {
                SetHint("Elegí cuántos mazos usar");
                return;
            }

            if (playerCount < MinPlayersToStart)
            {
                SetHint($"Se necesitan al menos {MinPlayersToStart} jugadores");
                return;
            }

            string roomId = GameManager.Instance?.CurrentRoomId;
            if (string.IsNullOrWhiteSpace(roomId))
            {
                Debug.LogError("[Lobby] CurrentRoomId vacío — no se puede iniciar");
                return;
            }

            try
            {
                LobbyPanelPresenter.StyleStartButton(startGameButton, false);
                await NetworkManager.Instance.StartGame(roomId);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Lobby] Error al iniciar juego: {ex.Message}");
                RefreshStartButton(GameManager.Instance?.CurrentGameState);
            }
        }

        private void OnGameStarted(string roomId)
        {
            SetSelectorVisible(false);
        }

        private void OnGameStateChanged(GameState newState)
        {
            UpdateUI(newState);
        }

        private void UpdateUI(GameState gameState)
        {
            ApplyLayout();

            if (gameState == null)
            {
                SetSelectorVisible(false);
                return;
            }

            isRoomCreator = gameState.isRoomCreator;
            bool inLobby = !gameState.isGameStarted;
            SetSelectorVisible(inLobby);
            if (!inLobby) return;

            string roomId = gameState.roomId;
            if (string.IsNullOrEmpty(roomId))
                roomId = GameManager.Instance?.CurrentRoomId ?? "???";

            if (titleText != null)
                titleText.text = $"Lobby · {roomId}";

            UpdatePlayersList(gameState);
            UpdateCreatorControls(gameState);

            if (gameState.gameMode != null)
            {
                selectedDeckCount = gameState.gameMode.deckCount;
                UpdateDeckButtonsVisuals();
                UpdateModeInfo(gameState.gameMode);
            }
            else
            {
                UpdateModeInfoFromSelection();
            }

            RefreshStartButton(gameState);
        }

        private void UpdateCreatorControls(GameState gameState)
        {
            bool creator = gameState.isRoomCreator;

            if (deck1Button != null) deck1Button.gameObject.SetActive(creator);
            if (deck2Button != null) deck2Button.gameObject.SetActive(creator);
            if (deck3Button != null) deck3Button.gameObject.SetActive(creator);
            if (startGameButton != null) startGameButton.gameObject.SetActive(creator);

            if (!creator)
            {
                SetHint("Esperando a que el anfitrión inicie la partida…");
                if (modeInfoText != null && gameState.gameMode == null)
                    modeInfoText.text = "El anfitrión elige los mazos";
            }
        }

        private void RefreshStartButton(GameState gameState)
        {
            int playerCount = gameState?.players?.Count ?? 0;
            bool canStart = isRoomCreator
                            && selectedDeckCount > 0
                            && playerCount >= MinPlayersToStart
                            && !(gameState?.isGameStarted ?? true);

            LobbyPanelPresenter.StyleStartButton(startGameButton, canStart);

            if (!isRoomCreator) return;

            if (playerCount < MinPlayersToStart)
                SetHint($"Esperando jugadores… ({playerCount}/{MinPlayersToStart} mínimo)");
            else if (selectedDeckCount == 0)
                SetHint("Elegí 1, 2 o 3 mazos para la partida");
            else
                SetHint("Listo — podés iniciar la partida");
        }

        private void SetHint(string message)
        {
            if (waitingHintText != null)
                waitingHintText.text = message;
        }

        private void SetSelectorVisible(bool visible)
        {
            if (selectorPanel != null && selectorPanel != gameObject)
            {
                selectorPanel.SetActive(visible);
                return;
            }

            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        private void UpdatePlayersList(GameState gameState)
        {
            if (playersInfoText == null) return;

            var players = gameState.players;
            int count = players?.Count ?? 0;
            string myId = NetworkManager.Instance?.MyConnectionId;

            var sb = new StringBuilder();
            sb.AppendLine($"<b>Jugadores ({count}/8)</b>");

            if (count == 0)
            {
                sb.AppendLine("Nadie en la sala todavía");
                playersInfoText.text = sb.ToString();
                return;
            }

            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                bool isMe = !string.IsNullOrEmpty(myId) && p.connectionId == myId;
                bool isHost = i == 0; // orden de ingreso: el primero creó la sala

                string tags = "";
                if (isHost) tags += " · anfitrión";
                if (isMe) tags += " · vos";

                sb.AppendLine($"{i + 1}. {p.name}{tags}");
            }

            playersInfoText.text = sb.ToString().TrimEnd();
        }

        private void UpdateModeInfo(GameMode mode)
        {
            if (modeInfoText == null || mode == null) return;

            int playerCount = GameManager.Instance?.CurrentGameState?.players?.Count ?? 1;
            if (playerCount < 1) playerCount = 1;
            int cardsPerPlayer = mode.cardsPerPlayer > 0
                ? mode.cardsPerPlayer
                : Mathf.FloorToInt((mode.deckCount * 40f) / playerCount);

            modeInfoText.text =
                $"{mode.deckCount} mazo{(mode.deckCount > 1 ? "s" : "")} · " +
                $"{mode.deckCount * 40} cartas · ~{cardsPerPlayer} por jugador";
        }

        private void UpdateModeInfoFromSelection()
        {
            if (modeInfoText == null) return;

            if (selectedDeckCount <= 0)
            {
                if (isRoomCreator)
                    modeInfoText.text = "Seleccioná cantidad de mazos";
                return;
            }

            int playerCount = GameManager.Instance?.CurrentGameState?.players?.Count ?? 1;
            if (playerCount < 1) playerCount = 1;
            int cardsPerPlayer = Mathf.FloorToInt((selectedDeckCount * 40f) / playerCount);
            modeInfoText.text =
                $"{selectedDeckCount} mazo{(selectedDeckCount > 1 ? "s" : "")} · " +
                $"{selectedDeckCount * 40} cartas · ~{cardsPerPlayer} por jugador";
        }

        private void UpdateDeckButtonsVisuals()
        {
            UpdateButtonColor(deck1Button, selectedDeckCount == 1);
            UpdateButtonColor(deck2Button, selectedDeckCount == 2);
            UpdateButtonColor(deck3Button, selectedDeckCount == 3);
        }

        private void UpdateButtonColor(Button button, bool isSelected)
        {
            if (button == null) return;
            var colors = button.colors;
            colors.normalColor = isSelected ? selectedColor : normalColor;
            colors.highlightedColor = isSelected ? selectedColor : normalColor;
            colors.selectedColor = selectedColor;
            button.colors = colors;
        }

        private void OnDestroy()
        {
            if (deck1Button != null) deck1Button.onClick.RemoveAllListeners();
            if (deck2Button != null) deck2Button.onClick.RemoveAllListeners();
            if (deck3Button != null) deck3Button.onClick.RemoveAllListeners();
            if (startGameButton != null) startGameButton.onClick.RemoveListener(OnStartGameClicked);

            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;

            if (NetworkManager.Instance != null)
                NetworkManager.Instance.OnGameStarted -= OnGameStarted;
        }
    }
}
