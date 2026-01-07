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
    /// UI principal del juego (durante la partida)
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI roomInfoText;
        [SerializeField] private TextMeshProUGUI turnInfoText;
        [SerializeField] private TextMeshProUGUI notificationText;
        [SerializeField] private Button playCardsButton;
        [SerializeField] private Button passTurnButton;
        [SerializeField] private GameObject pepineadoEffectPanel;
        [SerializeField] private Transform playersListContainer;
        [SerializeField] private GameObject playerInfoPrefab;

        [Header("Controllers")]
        [SerializeField] private HandManager handManager;
        [SerializeField] private TableManager tableManager;

        private Dictionary<string, GameObject> playerInfoObjects = new Dictionary<string, GameObject>();

        private void Start()
        {
            // Configurar botones
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

            // Ocultar efecto PEPINEADO
            if (pepineadoEffectPanel != null)
                pepineadoEffectPanel.SetActive(false);

            // Suscribirse a eventos
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
                GameManager.Instance.OnHandUpdated += OnHandUpdated;
                GameManager.Instance.OnNotification += OnNotification;
            }

            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnCardsPlayed += OnCardsPlayed;
                NetworkManager.Instance.OnPlayerWon += OnPlayerWon;
            }
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
            {
                handManager.UpdateHand(hand);
            }
        }

        private void OnCardsPlayed(PlayedCards playedCards)
        {
            // Agregar cartas a la mesa
            if (tableManager != null)
            {
                tableManager.AddCardsToTable(playedCards.cards, playedCards.playerName);
            }

            // Mostrar efecto PEPINEADO si aplica
            if (playedCards.isPepineado)
            {
                ShowPepineadoEffect(playedCards.playerName);
            }
        }

        private void OnPlayerWon(string playerName)
        {
            ShowNotification($"🏆 ¡{playerName} ha ganado!", 5f);
        }

        private void OnNotification(string message)
        {
            ShowNotification(message, 3f);
        }

        private void UpdateRoomInfo(GameState gameState)
        {
            if (roomInfoText == null) return;

            string roomId = gameState.roomId ?? "???";
            int playerCount = gameState.players?.Count ?? 0;
            
            roomInfoText.text = $"🎮 Sala: {roomId} | 👥 Jugadores: {playerCount}";
        }

        private void UpdateTurnInfo(GameState gameState)
        {
            if (turnInfoText == null) return;

            var currentPlayer = gameState.GetCurrentPlayer();
            bool isMyTurn = gameState.IsMyTurn(NetworkManager.Instance.MyConnectionId);

            if (currentPlayer != null)
            {
                string turnText = isMyTurn 
                    ? "🎯 ¡TU TURNO!" 
                    : $"⏳ Turno de: {currentPlayer.name}";
                
                turnInfoText.text = turnText;
            }
            else
            {
                turnInfoText.text = "⏳ Esperando...";
            }
        }

        private void UpdatePlayersList(GameState gameState)
        {
            if (playersListContainer == null || playerInfoPrefab == null) return;

            // Limpiar lista existente
            foreach (var obj in playerInfoObjects.Values)
            {
                if (obj != null) Destroy(obj);
            }
            playerInfoObjects.Clear();

            // Crear info para cada jugador
            if (gameState.players != null)
            {
                foreach (var player in gameState.players)
                {
                    GameObject infoObj = Instantiate(playerInfoPrefab, playersListContainer);
                    TextMeshProUGUI infoText = infoObj.GetComponentInChildren<TextMeshProUGUI>();
                    
                    if (infoText != null)
                    {
                        string status = "";
                        if (player.hasWon) status = "🏆";
                        else if (player.isCurrentTurn) status = "🎯";
                        else if (player.isSkipped) status = "⏭️";

                        infoText.text = $"{status} {player.name} ({player.cardCount})";
                    }

                    playerInfoObjects[player.connectionId] = infoObj;
                }
            }
        }

        private void UpdateButtonsState(GameState gameState)
        {
            bool isMyTurn = gameState.IsMyTurn(NetworkManager.Instance.MyConnectionId);
            bool hasSelectedCards = GameManager.Instance.SelectedCards.Count > 0;
            bool isFirstPlay = gameState.IsFirstPlay();

            if (playCardsButton != null)
            {
                playCardsButton.interactable = isMyTurn && hasSelectedCards;
            }

            if (passTurnButton != null)
            {
                passTurnButton.interactable = isMyTurn && !isFirstPlay;
            }
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

            // Mostrar efecto
            pepineadoEffectPanel.SetActive(true);

            // Actualizar texto si hay
            var effectText = pepineadoEffectPanel.GetComponentInChildren<TextMeshProUGUI>();
            if (effectText != null)
            {
                effectText.text = $"🥒 ¡PEPINEADO!\n{playerName}";
            }

            // Animar el efecto en la mesa
            if (tableManager != null)
            {
                tableManager.ShowPepineadoEffect();
            }

            // Ocultar después de 3 segundos
            Invoke(nameof(HidePepineadoEffect), 3f);
        }

        private void HidePepineadoEffect()
        {
            if (pepineadoEffectPanel != null)
            {
                pepineadoEffectPanel.SetActive(false);
            }
        }

        private void ShowNotification(string message, float duration)
        {
            if (notificationText == null) return;

            notificationText.text = message;
            notificationText.gameObject.SetActive(true);

            // Cancelar invocaciones previas
            CancelInvoke(nameof(HideNotification));
            
            // Ocultar después de duration segundos
            Invoke(nameof(HideNotification), duration);
        }

        private void HideNotification()
        {
            if (notificationText != null)
            {
                notificationText.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            // Actualizar botones cada frame (por si el jugador selecciona/deselecciona cartas)
            if (GameManager.Instance?.CurrentGameState != null)
            {
                UpdateButtonsState(GameManager.Instance.CurrentGameState);
            }
        }

        private void OnDestroy()
        {
            if (playCardsButton != null)
                playCardsButton.onClick.RemoveListener(OnPlayCardsClicked);
            
            if (passTurnButton != null)
                passTurnButton.onClick.RemoveListener(OnPassTurnClicked);
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
                GameManager.Instance.OnHandUpdated -= OnHandUpdated;
                GameManager.Instance.OnNotification -= OnNotification;
            }

            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnCardsPlayed -= OnCardsPlayed;
                NetworkManager.Instance.OnPlayerWon -= OnPlayerWon;
            }
        }
    }
}

