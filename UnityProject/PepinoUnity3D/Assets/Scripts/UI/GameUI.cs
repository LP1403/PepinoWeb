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
    /// Main in-game HUD: match sidebar + turn + play/pass.
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
        private bool lastStarted;

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

            DisableFullScreenRaycasts();
            EnsurePlayersInfoText();

            bool started = GameManager.Instance?.CurrentGameState?.isGameStarted ?? false;
            GameHudPresenter.Apply(
                roomInfoText,
                turnInfoText,
                notificationText,
                playersInfoText,
                playCardsButton,
                passTurnButton,
                started);
            lastStarted = started;

            BindEvents();
            ApplyCurrentState();
        }

        private void DisableFullScreenRaycasts()
        {
            var panelImage = GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = new Color(1f, 1f, 1f, 0f);
                panelImage.raycastTarget = false;
            }

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
            playersInfoText = go.AddComponent<TextMeshProUGUI>();
            playersInfoText.text = "Jugadores:";
        }

        private void OnNewRound()
        {
            if (tableManager != null)
                tableManager.ClearTable();
        }

        private void OnGameStateChanged(GameState gameState)
        {
            if (gameState == null) return;

            try
            {
                bool started = gameState.isGameStarted;
                // Always re-apply HUD layout while in-game so runtime fixes stick after hot reload
                GameHudPresenter.Apply(
                    roomInfoText,
                    turnInfoText,
                    notificationText,
                    playersInfoText,
                    playCardsButton,
                    passTurnButton,
                    started);
                lastStarted = started;

                UpdateRoomInfo(gameState);
                UpdateTurnInfo(gameState);
                UpdateMatchPlayersSidebar(gameState);
                UpdateButtonsState(gameState);
                SyncTableFromState(gameState);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameUI] OnGameStateChanged: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Keep discard pile in sync with server lastPlayedCards (authoritative).
        /// Avoids empty table when OnNewRound falsely cleared or CardsPlayed was missed.
        /// </summary>
        private void SyncTableFromState(GameState gameState)
        {
            if (tableManager == null || gameState == null || !gameState.isGameStarted)
                return;

            tableManager.SyncDiscardPile(
                gameState.lastPlayedCards,
                animate: true,
                fromPlayerId: gameState.lastPlayerId);
        }

        private void OnHandUpdated(List<Card> hand)
        {
            try
            {
                if (handManager != null)
                    handManager.UpdateHand(hand);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameUI] OnHandUpdated: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void OnCardsPlayed(PlayedCards playedCards)
        {
            if (playedCards == null) return;

            try
            {
                if (tableManager != null && playedCards.cards != null)
                {
                    tableManager.SyncDiscardPile(
                        playedCards.cards,
                        animate: true,
                        fromPlayerId: playedCards.playerId);
                }

                if (playedCards.isPepineado)
                    ShowPepineadoEffect(playedCards.playerName, playedCards.playerId);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameUI] OnCardsPlayed: {ex.Message}\n{ex.StackTrace}");
            }
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
            if (roomInfoText == null || gameState == null || !gameState.isGameStarted) return;

            string roomId = gameState.roomId;
            if (string.IsNullOrEmpty(roomId))
                roomId = GameManager.Instance?.CurrentRoomId;
            if (string.IsNullOrEmpty(roomId))
                roomId = "???";

            roomInfoText.text = $"PARTIDA EN CURSO\nSala {roomId}";
        }

        private void UpdateTurnInfo(GameState gameState)
        {
            if (turnInfoText == null) return;

            if (!gameState.isGameStarted)
            {
                turnInfoText.gameObject.SetActive(false);
                return;
            }

            var currentPlayer = gameState.GetCurrentPlayer();
            bool isMyTurn = NetworkManager.Instance != null
                && gameState.IsMyTurn(NetworkManager.Instance.MyConnectionId);
            bool freePlay = GameManager.Instance != null && GameManager.Instance.IsFreePlayRound();

            string message;
            if (currentPlayer == null)
                message = "Esperando…";
            else if (isMyTurn && freePlay)
                message = "NUEVA RONDA\nJuega libremente";
            else if (isMyTurn)
                message = "ES TU TURNO";
            else
                message = $"Turno de {currentPlayer.name}";

            GameHudPresenter.SetTurnBannerState(turnInfoText, isMyTurn, message, freePlay);
        }

        private void UpdateMatchPlayersSidebar(GameState gameState)
        {
            if (playersInfoText == null) return;

            if (!gameState.isGameStarted)
            {
                playersInfoText.gameObject.SetActive(false);
                return;
            }

            playersInfoText.gameObject.SetActive(true);
            string myId = NetworkManager.Instance?.MyConnectionId;
            playersInfoText.text = GameHudPresenter.BuildPlayersSidebar(gameState, myId);

            if (playersListContainer == null || playerInfoPrefab == null) return;

            foreach (var obj in playerInfoObjects.Values)
            {
                if (obj != null) Destroy(obj);
            }

            playerInfoObjects.Clear();

            if (gameState.players == null) return;

            for (int i = 0; i < gameState.players.Count; i++)
            {
                var player = gameState.players[i];
                if (player == null) continue;

                GameObject infoObj = Instantiate(playerInfoPrefab, playersListContainer);
                TextMeshProUGUI infoText = infoObj.GetComponentInChildren<TextMeshProUGUI>();
                if (infoText != null)
                    infoText.text = $"{player.name} ({player.cardCount})";

                string key = !string.IsNullOrEmpty(player.connectionId)
                    ? player.connectionId
                    : $"p{i}";
                playerInfoObjects[key] = infoObj;
            }
        }

        private void UpdateButtonsState(GameState gameState)
        {
            bool started = gameState != null && gameState.isGameStarted;

            if (playCardsButton != null)
                playCardsButton.gameObject.SetActive(started);
            if (passTurnButton != null)
                passTurnButton.gameObject.SetActive(started);

            if (!started) return;
            if (NetworkManager.Instance == null || GameManager.Instance == null) return;

            bool isMyTurn = gameState.IsMyTurn(NetworkManager.Instance.MyConnectionId);
            var selected = GameManager.Instance.SelectedCards;
            bool hasSelectedCards = selected != null && selected.Count > 0;
            bool playLegal = hasSelectedCards && GameManager.Instance.ValidatePlay(selected);
            bool canPass = isMyTurn && !gameState.IsFirstPlay();

            if (playCardsButton != null)
                playCardsButton.interactable = isMyTurn && playLegal;

            if (passTurnButton != null)
                passTurnButton.interactable = canPass;
        }

        private void OnPlayCardsClicked()
        {
            if (GameManager.Instance == null) return;

            var selected = GameManager.Instance.SelectedCards;
            if (selected != null && selected.Count > 0
                && !GameManager.Instance.TryValidatePlay(selected, out string reason)
                && !string.IsNullOrEmpty(reason))
            {
                ShowNotification(reason, 2.5f);
            }

            GameManager.Instance.PlaySelectedCards();
        }

        private void OnPassTurnClicked()
        {
            GameManager.Instance.PassTurn();
        }

        private void ShowPepineadoEffect(string playerName, string playerId = null)
        {
            string myId = NetworkManager.Instance?.MyConnectionId;
            if (string.IsNullOrEmpty(playerId))
            {
                var state = GameManager.Instance?.CurrentGameState;
                if (state?.players != null)
                {
                    foreach (var p in state.players)
                    {
                        if (p != null && p.name == playerName)
                        {
                            playerId = p.connectionId;
                            break;
                        }
                    }
                }
            }

            PepineadoOverlay.Show(playerName, myId, playerId);

            if (tableManager != null)
                tableManager.ShowPepineadoEffect();

            if (pepineadoEffectPanel != null)
                pepineadoEffectPanel.SetActive(false);
        }

        private void HidePepineadoEffect()
        {
            PepineadoOverlay.Hide();
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
    }
}
