using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PepinoGame.Models;
using PepinoGame.Controllers;
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

            var existingCard = selectedCards.FirstOrDefault(c => SameCard(c, card));

            if (existingCard != null)
            {
                selectedCards.Remove(existingCard);
                Log($"Carta deseleccionada: {card}");
            }
            else
            {
                if (selectedCards.Count > 0 && selectedCards[0] != null && selectedCards[0].value != card.value)
                {
                    Notify("⚠️ Debes jugar cartas del mismo valor");
                    return;
                }

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
            if (card == null) return false;
            return selectedCards.Any(c => SameCard(c, card));
        }

        private static bool SameCard(Card a, Card b)
        {
            if (a == null || b == null) return false;
            if (ReferenceEquals(a, b)) return true;
            if (!string.IsNullOrEmpty(a.id) && !string.IsNullOrEmpty(b.id))
                return a.id == b.id;
            return false;
        }

        /// <summary>
        /// Intenta jugar las cartas seleccionadas
        /// </summary>
        public async void PlaySelectedCards()
        {
            if (selectedCards.Count == 0)
            {
                Notify("No has seleccionado ninguna carta");
                return;
            }

            if (currentGameState == null || NetworkManager.Instance == null)
            {
                Notify("Sin conexión / estado");
                return;
            }

            if (!currentGameState.IsMyTurn(NetworkManager.Instance.MyConnectionId))
            {
                Notify("No es tu turno");
                return;
            }

            if (string.IsNullOrWhiteSpace(currentRoomId))
            {
                Notify("Sala inválida");
                return;
            }

            if (!TryValidatePlay(selectedCards, out string reason))
            {
                Notify(string.IsNullOrEmpty(reason) ? "Jugada inválida" : reason);
                return;
            }

            try
            {
                var toPlay = new List<Card>(selectedCards);
                if (toPlay.Any(c => c == null || string.IsNullOrEmpty(c.id)))
                {
                    Notify("Cartas sin id — reconectá / reiniciá la partida");
                    LogError("PlaySelectedCards: alguna carta seleccionada no tiene id válido");
                    return;
                }

                Log($"Jugando {toPlay.Count} carta(s) en sala {currentRoomId}: {string.Join(", ", toPlay)}");
                await NetworkManager.Instance.PlayCards(currentRoomId, toPlay);
                ClearCardSelection();
                if (Object.FindAnyObjectByType<HandManager>() is HandManager hm)
                    hm.DeselectAllCards();
            }
            catch (System.Exception ex)
            {
                LogError($"Error al jugar cartas: {ex.Message}\n{ex.StackTrace}");
                Notify($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Pasa el turno
        /// </summary>
        public async void PassTurn()
        {
            if (currentGameState == null || NetworkManager.Instance == null)
            {
                Notify("Sin conexión / estado");
                return;
            }

            if (!currentGameState.IsMyTurn(NetworkManager.Instance.MyConnectionId))
            {
                Notify("No es tu turno");
                return;
            }

            if (currentGameState.IsFirstPlay())
            {
                Notify("No podés pasar en la primera jugada");
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
                Notify($"Error: {ex.Message}");
            }
        }

        /// <summary>Validación optimista espejo del backend CardService.</summary>
        public bool ValidatePlay(List<Card> cards) => TryValidatePlay(cards, out _);

        public bool TryValidatePlay(List<Card> cards, out string reason)
        {
            reason = null;
            if (cards == null || cards.Count == 0)
            {
                reason = "Seleccioná al menos una carta";
                return false;
            }

            if (currentGameState == null)
            {
                reason = "Sin estado de juego";
                return false;
            }

            int firstValue = cards[0].value;
            if (!cards.All(c => c != null && c.value == firstValue))
            {
                reason = "Todas las cartas deben tener el mismo valor";
                return false;
            }

            // Primera jugada de la partida: libre
            if (currentGameState.IsFirstPlay())
            {
                Log("✅ Validación: Primera jugada - Válida");
                return true;
            }

            // Vuelta completa: el último que jugó vuelve a tener mano libre
            if (currentGameState.isNewRound)
            {
                Log("✅ Validación: Nueva ronda - Válida (juega libremente)");
                return true;
            }

            var lastPlayed = currentGameState.lastPlayedCards;
            if (lastPlayed == null || lastPlayed.Count == 0)
                return true;

            // Comodín (2): siempre jugable, cualquier cantidad — reinicia la jugada libre
            if (firstValue == 2)
            {
                Log("✅ Validación: Comodín (2) - Válida (jugada libre)");
                return true;
            }

            if (cards.Count != lastPlayed.Count)
            {
                reason = $"Debés jugar exactamente {lastPlayed.Count} carta(s)";
                Log($"❌ Validación: Cantidad incorrecta ({cards.Count} vs {lastPlayed.Count})");
                return false;
            }

            int lastValue = GetCardComparisonValue(lastPlayed[0]);
            int currentValue = GetCardComparisonValue(cards[0]);

            if (currentValue < lastValue)
            {
                reason = $"Valor insuficiente ({FormatRank(cards[0])} no supera a {FormatRank(lastPlayed[0])})";
                Log($"❌ Validación: Valor insuficiente ({currentValue} < {lastValue})");
                return false;
            }

            Log("✅ Validación: Jugada válida");
            return true;
        }

        private static string FormatRank(Card card)
        {
            if (card == null) return "?";
            return card.value switch
            {
                1 => "As",
                2 => "2 (comodín)",
                _ => card.value.ToString()
            };
        }

        /// <summary>
        /// Obtiene el valor de comparación de una carta (mismo que el backend)
        /// </summary>
        private int GetCardComparisonValue(Card card)
        {
            if (card == null) return 0;
            if (card.value == 2) return 0;  // Comodín (más bajo en jerarquía de comparación)
            if (card.value == 1) return 13; // As es el más alto
            return card.value;
        }

        /// <summary>True when it's my turn and the circle came back to me (free play).</summary>
        public bool IsFreePlayRound()
        {
            if (currentGameState == null || NetworkManager.Instance == null) return false;
            if (!currentGameState.isGameStarted) return false;
            if (currentGameState.IsFirstPlay()) return false;
            if (!currentGameState.isNewRound) return false;
            return currentGameState.IsMyTurn(NetworkManager.Instance.MyConnectionId);
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

            // Only clear table when backend RoundNumber bumps (real new deal),
            // NOT on isNewRound (that flag means "free play after full circle" — cards stay on table).
            if (newState.isGameStarted && roundChanged)
                OnNewRound?.Invoke();

            lastKnownRoundNumber = newState.roundNumber;
            lastWasNewRound = newState.isNewRound;
            currentGameState = newState;

            Log($"Estado actualizado - Sala: {newState.roomId}, Jugadores: {newState.players?.Count ?? 0}, Iniciado: {newState.isGameStarted}");

            SafeInvoke(OnGameStateChanged, newState);
            if (newState.yourHand != null)
                SafeInvoke(OnHandUpdated, newState.yourHand);
        }

        private void HandleCardsDealt(List<Card> hand)
        {
            if (hand == null) return;

            if (currentGameState == null)
                currentGameState = new GameState();

            currentGameState.yourHand = hand;
            Log($"Cartas recibidas: {hand.Count}");

            SafeInvoke(OnHandUpdated, hand);
        }

        private void HandleCardsPlayed(PlayedCards playedCards)
        {
            if (playedCards == null) return;

            int count = playedCards.cards?.Count ?? 0;
            Log($"{playedCards.playerName} jugó {count} carta(s)");

            if (playedCards.isPepineado)
                Log($"PEPINEADO por {playedCards.playerName}");
        }

        private void SafeInvoke<T>(System.Action<T> handlers, T arg)
        {
            if (handlers == null) return;

            foreach (System.Delegate d in handlers.GetInvocationList())
            {
                try
                {
                    ((System.Action<T>)d)?.Invoke(arg);
                }
                catch (System.Exception ex)
                {
                    LogError($"Listener {d.Method?.DeclaringType?.Name}.{d.Method?.Name} falló: {ex.Message}\n{ex.StackTrace}");
                }
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
            // Covered by Pepineado overlay when applicable — keep soft log only
            Log($"Jugador saltado: {playerName}");
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

            SafeInvoke(OnGameStateChanged, currentGameState);
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

        /// <summary>Short non-blocking hint while selecting an illegal combination.</summary>
        public void NotifyPlayHint(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            Notify(message);
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

