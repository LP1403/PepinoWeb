using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PepinoGame.Models;
using PepinoGame.Config;

namespace PepinoGame.Managers
{
    /// <summary>
    /// Maneja la lógica central del juego y el estado
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private GameConfig gameConfig;

        [Header("Current Game State")]
        [SerializeField] private string currentRoomId;
        [SerializeField] private string currentPlayerName;
        
        private GameState currentGameState;
        private List<Card> selectedCards = new List<Card>();
        private int lastKnownRoundNumber = -1;
        private bool lastWasNewRound;

        // Propiedades públicas
        public GameState CurrentGameState => currentGameState;
        public string CurrentRoomId => currentRoomId;
        public string CurrentPlayerName => currentPlayerName;
        public List<Card> SelectedCards => selectedCards;

        // Eventos
        public event System.Action<GameState> OnGameStateChanged;
        public event System.Action<List<Card>> OnHandUpdated;
        public event System.Action<string> OnNotification;
        public event System.Action OnNewRound;

        private bool networkEventsBound;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            currentGameState = new GameState();
        }

        private void OnEnable()
        {
            TryBindNetworkEvents();
        }

        private void Start()
        {
            TryBindNetworkEvents();
        }

        private void Update()
        {
            // NetworkManager may Awake after us on first frames
            if (!networkEventsBound)
                TryBindNetworkEvents();
        }

        private void TryBindNetworkEvents()
        {
            if (networkEventsBound || NetworkManager.Instance == null)
                return;

            NetworkManager.Instance.OnGameStateUpdated += HandleGameStateUpdated;
            NetworkManager.Instance.OnCardsDealt += HandleCardsDealt;
            NetworkManager.Instance.OnCardsPlayed += HandleCardsPlayed;
            NetworkManager.Instance.OnPlayerJoined += HandlePlayerJoined;
            NetworkManager.Instance.OnPlayerLeft += HandlePlayerLeft;
            NetworkManager.Instance.OnPlayerWon += HandlePlayerWon;
            NetworkManager.Instance.OnPlayerSkipped += HandlePlayerSkipped;
            NetworkManager.Instance.OnGameStarted += HandleGameStarted;
            NetworkManager.Instance.OnError += HandleError;
            networkEventsBound = true;
            Log("Suscripto a eventos de NetworkManager");
        }

        #region Public Methods

        /// <summary>
        /// Inicializa una nueva sesión de juego
        /// </summary>
        public void InitializeGame(string roomId, string playerName)
        {
            currentRoomId = roomId;
            currentPlayerName = playerName;
            selectedCards.Clear();
            
            Log($"Inicializando juego - Sala: {roomId}, Jugador: {playerName}");
        }

        /// <summary>
        /// Selecciona o deselecciona una carta
        /// </summary>
        public void ToggleCardSelection(Card card)
        {
            if (card == null) return;

            var existingCard = selectedCards.FirstOrDefault(c => c.id == card.id);
            
            if (existingCard != null)
            {
                // Deseleccionar
                selectedCards.Remove(existingCard);
                Log($"Carta deseleccionada: {card}");
            }
            else
            {
                // Verificar que todas las cartas seleccionadas tengan el mismo valor
                if (selectedCards.Count > 0 && selectedCards[0].value != card.value)
                {
                    Notify("⚠️ Debes jugar cartas del mismo valor");
                    return;
                }

                // Seleccionar
                selectedCards.Add(card);
                Log($"Carta seleccionada: {card}");
            }
        }

        /// <summary>
        /// Limpia la selección de cartas
        /// </summary>
        public void ClearCardSelection()
        {
            selectedCards.Clear();
            Log("Selección limpiada");
        }

        /// <summary>
        /// Verifica si una carta está seleccionada
        /// </summary>
        public bool IsCardSelected(Card card)
        {
            return selectedCards.Any(c => c.id == card.id);
        }

        /// <summary>
        /// Intenta jugar las cartas seleccionadas
        /// </summary>
        public async void PlaySelectedCards()
        {
            if (selectedCards.Count == 0)
            {
                Notify("⚠️ No has seleccionado ninguna carta");
                return;
            }

            if (!currentGameState.IsMyTurn(NetworkManager.Instance.MyConnectionId))
            {
                Notify("⚠️ No es tu turno");
                return;
            }

            if (!ValidatePlay(selectedCards))
            {
                Notify("⚠️ Jugada inválida");
                return;
            }

            try
            {
                Log($"Jugando {selectedCards.Count} carta(s)");
                await NetworkManager.Instance.PlayCards(currentRoomId, new List<Card>(selectedCards));
                ClearCardSelection();
            }
            catch (System.Exception ex)
            {
                LogError($"Error al jugar cartas: {ex.Message}");
                Notify($"❌ Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Pasa el turno
        /// </summary>
        public async void PassTurn()
        {
            if (!currentGameState.IsMyTurn(NetworkManager.Instance.MyConnectionId))
            {
                Notify("⚠️ No es tu turno");
                return;
            }

            if (currentGameState.IsFirstPlay())
            {
                Notify("⚠️ No puedes pasar en la primera jugada");
                return;
            }

            try
            {
                Log("Pasando turno");
                await NetworkManager.Instance.PassTurn(currentRoomId);
            }
            catch (System.Exception ex)
            {
                LogError($"Error al pasar turno: {ex.Message}");
                Notify($"❌ Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Valida si una jugada es válida
        /// </summary>
        public bool ValidatePlay(List<Card> cards)
        {
            if (cards == null || cards.Count == 0) return false;

            // Verificar que todas las cartas tengan el mismo valor
            int firstValue = cards[0].value;
            if (!cards.All(c => c.value == firstValue))
            {
                Log("❌ Validación: No todas las cartas tienen el mismo valor");
                return false;
            }

            // Si es la primera jugada o nueva ronda, cualquier carta es válida
            if (currentGameState.IsFirstPlay() || currentGameState.isNewRound)
            {
                Log("✅ Validación: Primera jugada o nueva ronda - Válida");
                return true;
            }

            var lastPlayed = currentGameState.lastPlayedCards;
            
            // Verificar que la cantidad de cartas sea la misma
            if (lastPlayed != null && lastPlayed.Count != cards.Count)
            {
                Log($"❌ Validación: Cantidad incorrecta ({cards.Count} vs {lastPlayed.Count})");
                return false;
            }

            // Verificar que el valor sea mayor o igual (para PEPINEADO)
            if (lastPlayed != null && lastPlayed.Count > 0)
            {
                int lastValue = GetCardComparisonValue(lastPlayed[0]);
                int currentValue = GetCardComparisonValue(cards[0]);
                
                if (currentValue < lastValue)
                {
                    Log($"❌ Validación: Valor insuficiente ({currentValue} < {lastValue})");
                    return false;
                }
            }

            Log("✅ Validación: Jugada válida");
            return true;
        }

        /// <summary>
        /// Obtiene el valor de comparación de una carta (mismo que el backend)
        /// </summary>
        private int GetCardComparisonValue(Card card)
        {
            if (card.value == 2) return 0;  // Comodín
            if (card.value == 1) return 13; // As es el más alto
            return card.value;
        }

        #endregion

        #region Event Handlers

        private void HandleGameStateUpdated(GameState newState)
        {
            if (newState == null) return;

            // Backend sometimes omits roomId in client view; keep the one from Join
            if (string.IsNullOrEmpty(newState.roomId) && !string.IsNullOrEmpty(currentRoomId))
                newState.roomId = currentRoomId;
            else if (!string.IsNullOrEmpty(newState.roomId))
                currentRoomId = newState.roomId;

            bool roundChanged = lastKnownRoundNumber >= 0 && newState.roundNumber != lastKnownRoundNumber;
            bool becameNewRound = newState.isNewRound && !lastWasNewRound;

            if (newState.isGameStarted && (roundChanged || becameNewRound))
                OnNewRound?.Invoke();

            lastKnownRoundNumber = newState.roundNumber;
            lastWasNewRound = newState.isNewRound;
            currentGameState = newState;

            Log($"Estado actualizado - Sala: {newState.roomId}, Jugadores: {newState.players?.Count ?? 0}, Iniciado: {newState.isGameStarted}");

            OnGameStateChanged?.Invoke(newState);
            OnHandUpdated?.Invoke(newState.yourHand);
        }

        private void HandleCardsDealt(List<Card> hand)
        {
            if (currentGameState.yourHand == null)
                currentGameState.yourHand = new List<Card>();
            
            currentGameState.yourHand = hand;
            Log($"Cartas recibidas: {hand.Count}");
            
            OnHandUpdated?.Invoke(hand);
        }

        private void HandleCardsPlayed(PlayedCards playedCards)
        {
            Log($"{playedCards.playerName} jugó {playedCards.cards.Count} carta(s)");
            
            if (playedCards.isPepineado)
            {
                Notify($"🥒 ¡PEPINEADO! {playedCards.playerName}");
            }
        }

        private void HandlePlayerJoined(string playerName)
        {
            Notify($"👤 {playerName} se unió a la sala");
        }

        private void HandlePlayerLeft(string playerName)
        {
            Notify($"👋 {playerName} salió de la sala");
        }

        private void HandlePlayerWon(string playerName)
        {
            Notify($"🏆 ¡{playerName} ha ganado!");
        }

        private void HandlePlayerSkipped(string playerName)
        {
            Notify($"⏭️ {playerName} fue saltado");
        }

        private void HandleGameStarted(string roomId)
        {
            lastKnownRoundNumber = -1;
            lastWasNewRound = false;

            if (!string.IsNullOrEmpty(roomId))
                currentRoomId = roomId;

            // Hide mode selector even if a later GameStateUpdated fails to deserialize
            if (currentGameState == null)
                currentGameState = new GameState();

            currentGameState.isGameStarted = true;
            if (string.IsNullOrEmpty(currentGameState.roomId))
                currentGameState.roomId = currentRoomId;

            OnGameStateChanged?.Invoke(currentGameState);
            OnNewRound?.Invoke();
            // Turn banner already communicates start — skip floating spam text
        }

        private void HandleError(string errorMessage)
        {
            Notify($"❌ {errorMessage}");
        }

        #endregion

        #region Utility Methods

        private void Notify(string message)
        {
            Log($"[Notificación] {message}");
            OnNotification?.Invoke(message);
        }

        private void Log(string message)
        {
            if (gameConfig.enableDebugLogs)
            {
                Debug.Log($"[GameManager] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[GameManager] {message}");
        }

        #endregion

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            if (networkEventsBound && NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnGameStateUpdated -= HandleGameStateUpdated;
                NetworkManager.Instance.OnCardsDealt -= HandleCardsDealt;
                NetworkManager.Instance.OnCardsPlayed -= HandleCardsPlayed;
                NetworkManager.Instance.OnPlayerJoined -= HandlePlayerJoined;
                NetworkManager.Instance.OnPlayerLeft -= HandlePlayerLeft;
                NetworkManager.Instance.OnPlayerWon -= HandlePlayerWon;
                NetworkManager.Instance.OnPlayerSkipped -= HandlePlayerSkipped;
                NetworkManager.Instance.OnGameStarted -= HandleGameStarted;
                NetworkManager.Instance.OnError -= HandleError;
                networkEventsBound = false;
            }
        }
    }
}

