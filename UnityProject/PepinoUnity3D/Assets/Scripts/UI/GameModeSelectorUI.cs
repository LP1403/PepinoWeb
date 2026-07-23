using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PepinoGame.Managers;
using PepinoGame.Models;

namespace PepinoGame.UI
{
    /// <summary>
    /// Left sidebar lobby before start: players, decks, start (≥2), copy room code.
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
        [SerializeField] private Color normalColor = new Color(0.85f, 0.87f, 0.9f, 1f);

        private int selectedDeckCount;
        private bool isRoomCreator;
        private bool layoutApplied;
        private TextMeshProUGUI waitingHintText;
        private TextMeshProUGUI statusText;
        private TextMeshProUGUI roomCodeText;
        private Button copyRoomButton;
        private RectTransform lobbyPlayerList;
        private string lastRoomId = "";

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
            EnsureExtraUi();
            ApplyLayout();
            WireButtons();
            UpdateUI(GameManager.Instance?.CurrentGameState);
        }

        private void WireButtons()
        {
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

            if (copyRoomButton != null)
                copyRoomButton.onClick.AddListener(OnCopyRoomClicked);

            RelabelDeck(deck1Button, "1 Mazo · 40 cartas");
            RelabelDeck(deck2Button, "2 Mazos · 80 cartas");
            RelabelDeck(deck3Button, "3 Mazos · 120 cartas");
        }

        private static void RelabelDeck(Button button, string label)
        {
            if (button == null) return;
            var tmp = button.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = label;
        }

        private void EnsureExtraUi()
        {
            var parent = selectorPanel != null ? selectorPanel.transform : transform;

            if (titleText == null)
                titleText = FindOrCreateTmp(parent, "LobbyTitle", "LOBBY");

            statusText = FindOrCreateTmp(parent, "LobbyStatus", "Esperando jugadores…");
            roomCodeText = FindOrCreateTmp(parent, "LobbyRoomCode", "Sala: ---");
            waitingHintText = FindOrCreateTmp(parent, "WaitingHint", "");

            // Own list — never share GameUI.playersInfoText (HUD hides it in lobby)
            lobbyPlayerList = LobbyPlayerCards.EnsureContainer(parent);
            if (playersInfoText != null)
                playersInfoText.gameObject.SetActive(false);

            if (copyRoomButton == null)
            {
                var existing = parent.Find("CopyRoomButton");
                if (existing != null)
                    copyRoomButton = existing.GetComponent<Button>();
            }

            if (copyRoomButton == null)
            {
                var go = new GameObject("CopyRoomButton", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(parent, false);
                var label = new GameObject("Text", typeof(RectTransform));
                label.transform.SetParent(go.transform, false);
                var tmp = label.AddComponent<TextMeshProUGUI>();
                tmp.text = "COPIAR";
                StretchFull(label.GetComponent<RectTransform>());
                copyRoomButton = go.GetComponent<Button>();
            }
        }

        private static TextMeshProUGUI FindOrCreateTmp(Transform parent, string name, string initial)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                var t = existing.GetComponent<TextMeshProUGUI>();
                if (t != null) return t;
            }

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = initial;
            return tmp;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void ApplyLayout()
        {
            var root = selectorPanel != null ? selectorPanel : gameObject;
            LobbyPanelPresenter.Apply(
                root,
                titleText,
                statusText,
                roomCodeText,
                null, // playersInfoText replaced by LobbyPlayerList cards
                modeInfoText,
                copyRoomButton,
                deck1Button,
                deck2Button,
                deck3Button,
                startGameButton,
                waitingHintText);

            if (lobbyPlayerList != null)
                LobbyPlayerCards.Place(lobbyPlayerList);

            layoutApplied = true;
        }

        private void OnCopyRoomClicked()
        {
            if (string.IsNullOrEmpty(lastRoomId)) return;
            GUIUtility.systemCopyBuffer = lastRoomId;
            SetHint("Código copiado");
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

        private void OnGameStarted(string roomId) => SetSelectorVisible(false);

        private void OnGameStateChanged(GameState newState) => UpdateUI(newState);

        private void UpdateUI(GameState gameState)
        {
            if (!layoutApplied)
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
            lastRoomId = roomId;

            if (titleText != null)
                titleText.text = "LOBBY";

            if (roomCodeText != null)
                roomCodeText.text = $"Partida privada · {roomId}";

            int count = gameState.players?.Count ?? 0;
            if (statusText != null)
            {
                statusText.text = count >= MinPlayersToStart
                    ? $"Listos · {count} / 8"
                    : $"Esperando jugadores… {count} / 8";
                statusText.color = count >= MinPlayersToStart
                    ? new Color(0.35f, 0.85f, 0.45f, 1f)
                    : new Color(0.9f, 0.78f, 0.35f, 1f);
            }

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
                SetHint($"Mínimo {MinPlayersToStart} jugadores ({playerCount}/{MinPlayersToStart})");
            else if (selectedDeckCount == 0)
                SetHint("Elegí 1, 2 o 3 mazos");
            else
                SetHint("Listo — podés iniciar");
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
            if (lobbyPlayerList == null)
            {
                var parent = selectorPanel != null ? selectorPanel.transform : transform;
                lobbyPlayerList = LobbyPlayerCards.EnsureContainer(parent);
                LobbyPlayerCards.Place(lobbyPlayerList);
            }

            string myId = NetworkManager.Instance?.MyConnectionId;
            LobbyPlayerCards.Rebuild(lobbyPlayerList, gameState?.players, myId);
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
                $"Ajustes: {mode.deckCount} mazo{(mode.deckCount > 1 ? "s" : "")} · " +
                $"~{cardsPerPlayer} cartas/jugador";
        }

        private void UpdateModeInfoFromSelection()
        {
            if (modeInfoText == null) return;

            if (selectedDeckCount <= 0)
            {
                if (isRoomCreator)
                    modeInfoText.text = "Ajustes de partida — elegí mazos";
                return;
            }

            int playerCount = GameManager.Instance?.CurrentGameState?.players?.Count ?? 1;
            if (playerCount < 1) playerCount = 1;
            int cardsPerPlayer = Mathf.FloorToInt((selectedDeckCount * 40f) / playerCount);
            modeInfoText.text =
                $"Ajustes: {selectedDeckCount} mazo{(selectedDeckCount > 1 ? "s" : "")} · " +
                $"~{cardsPerPlayer} cartas/jugador";
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

            var img = button.GetComponent<Image>();
            if (img != null)
                img.color = isSelected ? selectedColor : normalColor;
        }

        private void OnDestroy()
        {
            if (deck1Button != null) deck1Button.onClick.RemoveAllListeners();
            if (deck2Button != null) deck2Button.onClick.RemoveAllListeners();
            if (deck3Button != null) deck3Button.onClick.RemoveAllListeners();
            if (startGameButton != null) startGameButton.onClick.RemoveListener(OnStartGameClicked);
            if (copyRoomButton != null) copyRoomButton.onClick.RemoveListener(OnCopyRoomClicked);

            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;

            if (NetworkManager.Instance != null)
                NetworkManager.Instance.OnGameStarted -= OnGameStarted;
        }
    }
}
