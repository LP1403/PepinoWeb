using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PepinoGame.Managers;
using PepinoGame.Models;

namespace PepinoGame.UI
{
    /// <summary>
    /// Deck-count selector. Visible only to the room creator before the game starts.
    /// </summary>
    public class GameModeSelectorUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject selectorPanel;
        [SerializeField] private Button deck1Button;
        [SerializeField] private Button deck2Button;
        [SerializeField] private Button deck3Button;
        [SerializeField] private Button startGameButton;
        [SerializeField] private TextMeshProUGUI modeInfoText;
        [SerializeField] private TextMeshProUGUI playersInfoText;

        [Header("Colors")]
        [SerializeField] private Color selectedColor = Color.green;
        [SerializeField] private Color normalColor = Color.white;

        private int selectedDeckCount = 0;
        private bool isRoomCreator = false;

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
            if (deck1Button != null)
                deck1Button.onClick.AddListener(() => SelectDeckCount(1));

            if (deck2Button != null)
                deck2Button.onClick.AddListener(() => SelectDeckCount(2));

            if (deck3Button != null)
                deck3Button.onClick.AddListener(() => SelectDeckCount(3));

            if (startGameButton != null)
            {
                startGameButton.onClick.AddListener(OnStartGameClicked);
                startGameButton.interactable = false;
            }

            UpdateUI(GameManager.Instance?.CurrentGameState);
        }

        private async void SelectDeckCount(int deckCount)
        {
            if (GameManager.Instance?.CurrentGameState != null)
                isRoomCreator = GameManager.Instance.CurrentGameState.isRoomCreator;

            if (!isRoomCreator)
            {
                Debug.Log("[GameModeSelector] Solo el creador puede elegir el modo");
                return;
            }

            string roomId = GameManager.Instance?.CurrentRoomId;
            if (string.IsNullOrWhiteSpace(roomId))
            {
                Debug.LogError("[GameModeSelector] CurrentRoomId vacío — volvé a unirte a la sala");
                return;
            }

            selectedDeckCount = deckCount;
            UpdateDeckButtonsVisuals();

            try
            {
                await NetworkManager.Instance.SelectGameMode(roomId, deckCount);

                if (startGameButton != null)
                    startGameButton.interactable = true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameModeSelector] Error al seleccionar modo: {ex.Message}");
            }
        }

        private async void OnStartGameClicked()
        {
            if (!isRoomCreator)
                return;

            if (selectedDeckCount == 0)
            {
                Debug.Log("[GameModeSelector] Primero selecciona un modo de juego");
                return;
            }

            string roomId = GameManager.Instance?.CurrentRoomId;
            if (string.IsNullOrWhiteSpace(roomId))
            {
                Debug.LogError("[GameModeSelector] CurrentRoomId vacío — no se puede iniciar");
                return;
            }

            try
            {
                if (startGameButton != null)
                    startGameButton.interactable = false;

                await NetworkManager.Instance.StartGame(roomId);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameModeSelector] Error al iniciar juego: {ex.Message}");
                if (startGameButton != null)
                    startGameButton.interactable = true;
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
            if (gameState == null)
            {
                SetSelectorVisible(false);
                return;
            }

            isRoomCreator = gameState.isRoomCreator;
            bool shouldShow = isRoomCreator && !gameState.isGameStarted;
            SetSelectorVisible(shouldShow);

            if (!shouldShow) return;

            UpdatePlayersInfo(gameState);

            if (gameState.gameMode != null)
            {
                selectedDeckCount = gameState.gameMode.deckCount;
                UpdateDeckButtonsVisuals();
                UpdateModeInfo(gameState.gameMode);

                if (startGameButton != null)
                    startGameButton.interactable = true;
            }
        }

        private void SetSelectorVisible(bool visible)
        {
            // Scene wires selectorPanel to this same GameObject. Disabling it would kill
            // this script and drop event subscriptions — use CanvasGroup instead.
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

        private void UpdatePlayersInfo(GameState gameState)
        {
            if (playersInfoText == null) return;
            int playerCount = gameState.players?.Count ?? 0;
            playersInfoText.text = $"Jugadores: {playerCount}/8";
        }

        private void UpdateModeInfo(GameMode mode)
        {
            if (modeInfoText == null) return;

            int totalCards = mode.deckCount * 48;
            modeInfoText.text =
                $"{mode.deckCount} Mazo(s)\n" +
                $"{totalCards} cartas totales\n" +
                $"{mode.cardsPerPlayer} cartas por jugador";
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
