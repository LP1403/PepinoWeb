using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using PepinoGame.Managers;
using PepinoGame.Models;
using PepinoGame.Controllers;

namespace PepinoGame.UI
{
    /// <summary>
    /// Main in-game HUD.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI roomInfoText;
        [SerializeField] private TextMeshProUGUI turnInfoText;
        [SerializeField] private TextMeshProUGUI notificationText;
        [SerializeField] private TextMeshProUGUI playersInfoText;
        [SerializeField] private Button playCardsButton;
        [SerializeField] private Button passTurnButton;
        [SerializeField] private GameObject pepineadoEffectPanel;
        [SerializeField] private Transform playersListContainer;
        [SerializeField] private GameObject playerInfoPrefab;

        [Header("Controllers")]
        [SerializeField] private HandManager handManager;
        [SerializeField] private TableManager tableManager;

        private readonly Dictionary<string, GameObject> playerInfoObjects = new Dictionary<string, GameObject>();
        private bool gameManagerBound;
        private bool networkBound;

        private void OnEnable()
        {
            BindEvents();
            ApplyCurrentState();
        }

        private void Start()
        {
            if (playCardsButton != null)
            {
                playCardsButton.onClick.AddListener(OnPlayCardsClicked);
                playCardsButton.interactable = false;
            }

            if (passTurnButton != null)
            {
                passTurnButton.onClick.AddListener(OnPassTurnClicked);
                passTurnButton.interactable = false;
            }

            if (pepineadoEffectPanel != null)
                pepineadoEffectPanel.SetActive(false);

            // GamePanel often has a full-screen Image that blocks 3D clicks
            var panelImage = GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = new Color(1f, 1f, 1f, 0f);
                panelImage.raycastTarget = false;
            }

            // Cualquier Image full-screen hija también
            foreach (var img in GetComponentsInChildren<Image>(true))
            {
                if (img == null) continue;
                var rt = img.rectTransform;
                if (rt == null) continue;
                bool fullBleed = rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one
                                 && img.GetComponent<Button>() == null;
                if (fullBleed && img.color.a < 0.05f)
                    img.raycastTarget = false;
            }

            EnsurePlayersInfoText();
            GameHudPresenter.Apply(
                roomInfoText,
                turnInfoText,
                notificationText,
                playCardsButton,
                passTurnButton);

            // Hide legacy left-side player dump — seats are 3D around the table now
            if (playersInfoText != null && gameObject.activeInHierarchy)
                playersInfoText.gameObject.SetActive(false);

            BindEvents();
            ApplyCurrentState();
        }

        private void BindEvents()
        {
            if (!gameManagerBound && GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
                GameManager.Instance.OnHandUpdated -= OnHandUpdated;
                GameManager.Instance.OnNotification -= OnNotification;
                GameManager.Instance.OnNewRound -= OnNewRound;

                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
                GameManager.Instance.OnHandUpdated += OnHandUpdated;
                GameManager.Instance.OnNotification += OnNotification;
                GameManager.Instance.OnNewRound += OnNewRound;
                gameManagerBound = true;
            }

            if (!networkBound && NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnCardsPlayed -= OnCardsPlayed;
                NetworkManager.Instance.OnPlayerWon -= OnPlayerWon;

                NetworkManager.Instance.OnCardsPlayed += OnCardsPlayed;
                NetworkManager.Instance.OnPlayerWon += OnPlayerWon;
                networkBound = true;
            }
        }

        private void ApplyCurrentState()
        {
            var state = GameManager.Instance?.CurrentGameState;
            if (state == null) return;

            OnGameStateChanged(state);

            if (state.yourHand != null && state.yourHand.Count > 0)
                OnHandUpdated(state.yourHand);
        }

        private void EnsurePlayersInfoText()
        {
            if (playersInfoText != null) return;

            // Reuse existing PlayersInfoText in scene if present
            var existing = GameObject.Find("PlayersInfoText");
            if (existing != null)
            {
                playersInfoText = existing.GetComponent<TextMeshProUGUI>();
                if (playersInfoText != null) return;
            }

            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            var go = new GameObject("PlayersInfoText", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(24f, 40f);
            rect.sizeDelta = new Vector2(280f, 220f);

            playersInfoText = go.AddComponent<TextMeshProUGUI>();
            playersInfoText.fontSize = 18;
            playersInfoText.color = Color.white;
            playersInfoText.alignment = TextAlignmentOptions.TopLeft;
            playersInfoText.text = "Jugadores:";
        }

        private void OnNewRound()
        {
            if (tableManager != null)
                tableManager.ClearTable();
        }

        private void OnGameStateChanged(GameState gameState)
        {
            UpdateRoomInfo(gameState);
            UpdateTurnInfo(gameState);
            UpdatePlayersList(gameState);
            UpdateButtonsState(gameState);
        }

        private void OnHandUpdated(List<Card> hand)
        {
            if (handManager != null)
                handManager.UpdateHand(hand);
        }

        private void OnCardsPlayed(PlayedCards playedCards)
        {
            if (tableManager != null)
                tableManager.AddCardsToTable(playedCards.cards, playedCards.playerName);

            if (playedCards.isPepineado)
                ShowPepineadoEffect(playedCards.playerName);
        }

        private void OnPlayerWon(string playerName)
        {
            ShowNotification($"¡{playerName} ha ganado!", 5f);
        }

        private void OnNotification(string message)
        {
            ShowNotification(message, 3f);
        }

        private void UpdateRoomInfo(GameState gameState)
        {
            if (roomInfoText == null) return;

            string roomId = gameState.roomId;
            if (string.IsNullOrEmpty(roomId))
                roomId = GameManager.Instance?.CurrentRoomId;
            if (string.IsNullOrEmpty(roomId))
                roomId = "???";

            int playerCount = gameState.players?.Count ?? 0;
            roomInfoText.text = $"Sala: {roomId} | Jugadores: {playerCount}";
        }

        private void UpdateTurnInfo(GameState gameState)
        {
            if (turnInfoText == null) return;

            if (!gameState.isGameStarted)
            {
                // El lobby (GameModeSelector) ya muestra el estado; no duplicar banner
                turnInfoText.text = "";
                return;
            }

            var currentPlayer = gameState.GetCurrentPlayer();
            bool isMyTurn = gameState.IsMyTurn(NetworkManager.Instance.MyConnectionId);

            if (currentPlayer != null)
            {
                turnInfoText.text = isMyTurn
                    ? "¡TU TURNO!"
                    : $"Turno: {currentPlayer.name}";
                turnInfoText.color = isMyTurn
                    ? new Color(1f, 0.85f, 0.2f)
                    : new Color(0.95f, 0.95f, 0.95f);
            }
            else
            {
                turnInfoText.text = "Esperando...";
            }
        }

        private void UpdatePlayersList(GameState gameState)
        {
            // Opponent visuals live in OpponentSeatManager (3D seats). Keep prefab list path if wired.
            if (playersListContainer == null || playerInfoPrefab == null) return;

            foreach (var obj in playerInfoObjects.Values)
            {
                if (obj != null) Destroy(obj);
            }

            playerInfoObjects.Clear();

            if (gameState.players == null) return;

            foreach (var player in gameState.players)
            {
                GameObject infoObj = Instantiate(playerInfoPrefab, playersListContainer);
                TextMeshProUGUI infoText = infoObj.GetComponentInChildren<TextMeshProUGUI>();

                if (infoText != null)
                {
                    string status = "";
                    if (player.hasWon) status = "[GANO] ";
                    else if (player.isCurrentTurn) status = "> ";
                    else if (player.isSkipped) status = "[SKIP] ";

                    infoText.text = $"{status}{player.name} ({player.cardCount})";
                }

                playerInfoObjects[player.connectionId] = infoObj;
            }
        }

        private void UpdateButtonsState(GameState gameState)
        {
            bool started = gameState != null && gameState.isGameStarted;

            // En lobby no se muestran JUGAR / PASAR
            if (playCardsButton != null)
                playCardsButton.gameObject.SetActive(started);
            if (passTurnButton != null)
                passTurnButton.gameObject.SetActive(started);

            if (!started) return;

            bool isMyTurn = gameState.IsMyTurn(NetworkManager.Instance.MyConnectionId);
            bool hasSelectedCards = GameManager.Instance.SelectedCards.Count > 0;
            bool isFirstPlay = gameState.IsFirstPlay() || gameState.isNewRound;

            if (playCardsButton != null)
                playCardsButton.interactable = isMyTurn && hasSelectedCards;

            if (passTurnButton != null)
                passTurnButton.interactable = isMyTurn && !isFirstPlay;
        }

        private void OnPlayCardsClicked()
        {
            GameManager.Instance.PlaySelectedCards();
        }

        private void OnPassTurnClicked()
        {
            GameManager.Instance.PassTurn();
        }

        private void ShowPepineadoEffect(string playerName)
        {
            if (pepineadoEffectPanel == null) return;

            pepineadoEffectPanel.SetActive(true);

            var effectText = pepineadoEffectPanel.GetComponentInChildren<TextMeshProUGUI>();
            if (effectText != null)
                effectText.text = $"¡PEPINEADO!\n{playerName}";

            if (tableManager != null)
                tableManager.ShowPepineadoEffect();

            CancelInvoke(nameof(HidePepineadoEffect));
            Invoke(nameof(HidePepineadoEffect), 3f);
        }

        private void HidePepineadoEffect()
        {
            if (pepineadoEffectPanel != null)
                pepineadoEffectPanel.SetActive(false);
        }

        private void ShowNotification(string message, float duration)
        {
            if (notificationText == null) return;

            notificationText.text = message;
            notificationText.gameObject.SetActive(true);
            CancelInvoke(nameof(HideNotification));
            Invoke(nameof(HideNotification), duration);
        }

        private void HideNotification()
        {
            if (notificationText != null)
                notificationText.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (GameManager.Instance?.CurrentGameState != null)
                UpdateButtonsState(GameManager.Instance.CurrentGameState);
        }

        private void OnDestroy()
        {
            if (playCardsButton != null)
                playCardsButton.onClick.RemoveListener(OnPlayCardsClicked);

            if (passTurnButton != null)
                passTurnButton.onClick.RemoveListener(OnPassTurnClicked);

            if (gameManagerBound && GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
                GameManager.Instance.OnHandUpdated -= OnHandUpdated;
                GameManager.Instance.OnNotification -= OnNotification;
                GameManager.Instance.OnNewRound -= OnNewRound;
                gameManagerBound = false;
            }

            if (networkBound && NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnCardsPlayed -= OnCardsPlayed;
                NetworkManager.Instance.OnPlayerWon -= OnPlayerWon;
                networkBound = false;
            }
        }

        private void OnDisable()
        {
            // Keep subscriptions while GamePanel toggles; OnDestroy cleans up
        }
    }
}
