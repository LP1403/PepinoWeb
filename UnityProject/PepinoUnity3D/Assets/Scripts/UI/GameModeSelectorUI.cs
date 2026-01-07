using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PepinoGame.Managers;
using PepinoGame.Models;

namespace PepinoGame.UI
{
    /// <summary>
    /// UI para seleccionar el modo de juego (cantidad de mazos)
    /// Solo visible para el creador de la sala
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

        private void Start()
        {
            // Configurar botones
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

            // Suscribirse a eventos
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
            }

            // Estado inicial
            UpdateUI(null);
        }

        private async void SelectDeckCount(int deckCount)
        {
            if (!isRoomCreator)
            {
                Debug.Log("[GameModeSelector] Solo el creador puede seleccionar el modo");
                return;
            }

            selectedDeckCount = deckCount;
            UpdateDeckButtonsVisuals();

            try
            {
                await NetworkManager.Instance.SelectGameMode(
                    GameManager.Instance.CurrentRoomId, 
                    deckCount
                );

                if (startGameButton != null)
                    startGameButton.interactable = true;

                Debug.Log($"[GameModeSelector] Modo seleccionado: {deckCount} mazo(s)");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameModeSelector] Error al seleccionar modo: {ex.Message}");
            }
        }

        private async void OnStartGameClicked()
        {
            if (!isRoomCreator)
            {
                Debug.Log("[GameModeSelector] Solo el creador puede iniciar el juego");
                return;
            }

            if (selectedDeckCount == 0)
            {
                Debug.Log("[GameModeSelector] Primero debes seleccionar un modo de juego");
                return;
            }

            try
            {
                await NetworkManager.Instance.StartGame(GameManager.Instance.CurrentRoomId);
                Debug.Log("[GameModeSelector] Juego iniciado");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameModeSelector] Error al iniciar juego: {ex.Message}");
            }
        }

        private void OnGameStateChanged(GameState newState)
        {
            UpdateUI(newState);
        }

        private void UpdateUI(GameState gameState)
        {
            if (gameState == null)
            {
                if (selectorPanel != null)
                    selectorPanel.SetActive(false);
                return;
            }

            isRoomCreator = gameState.isRoomCreator;

            // Solo mostrar el selector si:
            // 1. Soy el creador
            // 2. El juego NO ha iniciado
            bool shouldShow = isRoomCreator && !gameState.isGameStarted;
            
            if (selectorPanel != null)
                selectorPanel.SetActive(shouldShow);

            if (!shouldShow) return;

            // Actualizar info de jugadores
            UpdatePlayersInfo(gameState);

            // Actualizar info del modo seleccionado
            if (gameState.gameMode != null)
            {
                selectedDeckCount = gameState.gameMode.deckCount;
                UpdateDeckButtonsVisuals();
                UpdateModeInfo(gameState.gameMode);
                
                if (startGameButton != null)
                    startGameButton.interactable = true;
            }
        }

        private void UpdatePlayersInfo(GameState gameState)
        {
            if (playersInfoText == null) return;

            int playerCount = gameState.players?.Count ?? 0;
            playersInfoText.text = $"👥 Jugadores: {playerCount}/8";
        }

        private void UpdateModeInfo(GameMode mode)
        {
            if (modeInfoText == null) return;

            int totalCards = mode.deckCount * 40;
            modeInfoText.text = $"📊 {mode.deckCount} Mazo(s)\n" +
                               $"🎴 {totalCards} cartas totales\n" +
                               $"👤 {mode.cardsPerPlayer} cartas por jugador";
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
            if (deck1Button != null)
                deck1Button.onClick.RemoveAllListeners();
            
            if (deck2Button != null)
                deck2Button.onClick.RemoveAllListeners();
            
            if (deck3Button != null)
                deck3Button.onClick.RemoveAllListeners();
            
            if (startGameButton != null)
                startGameButton.onClick.RemoveListener(OnStartGameClicked);
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
            }
        }
    }
}

