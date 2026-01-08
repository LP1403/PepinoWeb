using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Microsoft.AspNetCore.SignalR.Client;
using PepinoGame.Models;
using PepinoGame.Config;

namespace PepinoGame.Managers
{
    /// <summary>
    /// Maneja toda la comunicación con el backend SignalR
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private GameConfig gameConfig;

        private HubConnection connection;
        private bool isConnected = false;
        private string myConnectionId = string.Empty;

        // Eventos para que otros scripts se suscriban
        public event Action<bool> OnConnectionChanged;
        public event Action<GameState> OnGameStateUpdated;
        public event Action<List<Card>> OnCardsDealt;
        public event Action<PlayedCards> OnCardsPlayed;
        public event Action<string> OnPlayerJoined;
        public event Action<string> OnPlayerLeft;
        public event Action<string> OnPlayerWon;
        public event Action<string> OnPlayerSkipped;
        public event Action<string> OnGameStarted;
        public event Action<string> OnError;

        private void Awake()
        {
            // Singleton pattern
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
        }

        /// <summary>
        /// Conecta al servidor SignalR
        /// </summary>
        public async Task ConnectToServer()
        {
            if (connection != null && connection.State == HubConnectionState.Connected)
            {
                Log("Ya estoy conectado al servidor");
                return;
            }

            try
            {
                Log($"🔄 Conectando a SignalR: {gameConfig.serverUrl}");

                connection = new HubConnectionBuilder()
                    .WithUrl(gameConfig.serverUrl)
                    .WithAutomaticReconnect(GetReconnectDelays())
                    .Build();

                // Configurar eventos del hub
                SetupHubEvents();

                // Conectar
                await connection.StartAsync();
                
                myConnectionId = connection.ConnectionId;
                isConnected = true;

                Log($"✅ Conectado exitosamente! ConnectionId: {myConnectionId}");
                OnConnectionChanged?.Invoke(true);
            }
            catch (Exception ex)
            {
                LogError($"❌ Error al conectar: {ex.Message}");
                isConnected = false;
                OnConnectionChanged?.Invoke(false);
                throw;
            }
        }

        /// <summary>
        /// Configura todos los eventos que vienen del servidor
        /// </summary>
        private void SetupHubEvents()
        {
            // Evento: Estado del juego actualizado
            connection.On<GameState>("GameStateUpdated", (gameState) =>
            {
                Log($"🔄 Estado del juego actualizado");
                Log($"DEBUG RAW - RoomId: {gameState.roomId}");
                Log($"DEBUG RAW - isRoomCreator: {gameState.isRoomCreator}");
                Log($"DEBUG RAW - isGameStarted: {gameState.isGameStarted}");
                Log($"DEBUG RAW - players NULL? {gameState.players == null}");
                Log($"DEBUG RAW - players COUNT: {gameState.players?.Count ?? -1}");
                
                if (gameState.players != null && gameState.players.Count > 0)
                {
                    Log($"DEBUG RAW - Primer jugador: {gameState.players[0].name}");
                }
                
                OnGameStateUpdated?.Invoke(gameState);
            });

            // Evento: Cartas repartidas
            connection.On<List<Card>>("CardsDealt", (hand) =>
            {
                Log($"🎴 Cartas recibidas: {hand.Count}");
                OnCardsDealt?.Invoke(hand);
            });

            // Evento: Cartas jugadas
            connection.On<PlayedCards>("CardsPlayed", (playedCards) =>
            {
                Log($"🃏 {playedCards.playerName} jugó {playedCards.cards.Count} carta(s)");
                OnCardsPlayed?.Invoke(playedCards);
            });

            // Evento: Jugador se unió
            connection.On<string, int>("PlayerJoined", (playerName, playerCount) =>
            {
                Log($"👤 {playerName} se unió. Total: {playerCount}");
                OnPlayerJoined?.Invoke(playerName);
            });

            // Evento: Jugador se fue
            connection.On<string, int>("PlayerLeft", (playerName, playerCount) =>
            {
                Log($"👋 {playerName} se fue. Quedan: {playerCount}");
                OnPlayerLeft?.Invoke(playerName);
            });

            // Evento: Jugador ganó
            connection.On<string>("PlayerWon", (playerName) =>
            {
                Log($"🏆 {playerName} ganó!");
                OnPlayerWon?.Invoke(playerName);
            });

            // Evento: Jugador saltado (PEPINEADO)
            connection.On<string>("PlayerSkipped", (playerName) =>
            {
                Log($"⏭️ {playerName} fue saltado!");
                OnPlayerSkipped?.Invoke(playerName);
            });

            // Evento: Juego iniciado
            connection.On<string>("GameStarted", (roomId) =>
            {
                Log($"🎮 Juego iniciado en sala {roomId}");
                OnGameStarted?.Invoke(roomId);
            });

            // Evento: Error
            connection.On<string>("Error", (message) =>
            {
                LogError($"❌ Error del servidor: {message}");
                OnError?.Invoke(message);
            });

            // Manejar reconexión
            connection.Reconnecting += (error) =>
            {
                Log($"🔄 Reconectando...");
                isConnected = false;
                OnConnectionChanged?.Invoke(false);
                return Task.CompletedTask;
            };

            connection.Reconnected += (connectionId) =>
            {
                Log($"✅ Reconectado! ConnectionId: {connectionId}");
                myConnectionId = connectionId;
                isConnected = true;
                OnConnectionChanged?.Invoke(true);
                return Task.CompletedTask;
            };

            connection.Closed += async (error) =>
            {
                Log($"🔌 Conexión cerrada");
                isConnected = false;
                OnConnectionChanged?.Invoke(false);
                
                // Intentar reconectar después de 5 segundos
                await Task.Delay(5000);
                try
                {
                    await ConnectToServer();
                }
                catch
                {
                    LogError("No se pudo reconectar automáticamente");
                }
            };
        }

        #region Server Invocations (Llamadas al servidor)

        /// <summary>
        /// Unirse a una sala
        /// </summary>
        public async Task JoinRoom(string roomId, string playerName)
        {
            if (!isConnected)
            {
                LogError("No estás conectado al servidor");
                return;
            }

            try
            {
                Log($"🚪 Uniéndose a sala {roomId} como {playerName}");
                await connection.InvokeAsync("JoinRoom", roomId, playerName);
            }
            catch (Exception ex)
            {
                LogError($"Error al unirse a la sala: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Seleccionar modo de juego (solo creador)
        /// </summary>
        public async Task SelectGameMode(string roomId, int deckCount)
        {
            if (!isConnected) return;

            try
            {
                Log($"🎯 Seleccionando modo: {deckCount} mazos");
                await connection.InvokeAsync("SelectGameMode", roomId, deckCount);
            }
            catch (Exception ex)
            {
                LogError($"Error al seleccionar modo: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Iniciar el juego (solo creador)
        /// </summary>
        public async Task StartGame(string roomId)
        {
            if (!isConnected) return;

            try
            {
                Log($"🎮 Iniciando juego en sala {roomId}");
                await connection.InvokeAsync("StartGame", roomId);
            }
            catch (Exception ex)
            {
                LogError($"Error al iniciar juego: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Jugar cartas
        /// </summary>
        public async Task PlayCards(string roomId, List<Card> cards)
        {
            if (!isConnected) return;

            try
            {
                Log($"🃏 Jugando {cards.Count} carta(s)");
                await connection.InvokeAsync("PlayCards", roomId, cards);
            }
            catch (Exception ex)
            {
                LogError($"Error al jugar cartas: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Pasar turno
        /// </summary>
        public async Task PassTurn(string roomId)
        {
            if (!isConnected) return;

            try
            {
                Log("⏩ Pasando turno");
                await connection.InvokeAsync("PassTurn", roomId);
            }
            catch (Exception ex)
            {
                LogError($"Error al pasar turno: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Obtener estado actual del juego
        /// </summary>
        public async Task GetGameState(string roomId)
        {
            if (!isConnected) return;

            try
            {
                await connection.InvokeAsync("GetGameState", roomId);
            }
            catch (Exception ex)
            {
                LogError($"Error al obtener estado: {ex.Message}");
            }
        }

        /// <summary>
        /// Salir de la sala
        /// </summary>
        public async Task LeaveRoom(string roomId, string playerName)
        {
            if (!isConnected) return;

            try
            {
                Log($"🚪 Saliendo de sala {roomId}");
                await connection.InvokeAsync("LeaveRoom", roomId, playerName);
            }
            catch (Exception ex)
            {
                LogError($"Error al salir de sala: {ex.Message}");
            }
        }

        #endregion

        #region Utility Methods

        public bool IsConnected => isConnected;
        public string MyConnectionId => myConnectionId;

        private TimeSpan[] GetReconnectDelays()
        {
            var delays = new TimeSpan[gameConfig.reconnectionDelays.Length];
            for (int i = 0; i < gameConfig.reconnectionDelays.Length; i++)
            {
                delays[i] = TimeSpan.FromSeconds(gameConfig.reconnectionDelays[i]);
            }
            return delays;
        }

        private void Log(string message)
        {
            if (gameConfig.enableDebugLogs)
            {
                Debug.Log($"[NetworkManager] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[NetworkManager] {message}");
        }

        #endregion

        private async void OnDestroy()
        {
            if (connection != null)
            {
                try
                {
                    await connection.StopAsync();
                    await connection.DisposeAsync();
                }
                catch (Exception ex)
                {
                    LogError($"Error al cerrar conexión: {ex.Message}");
                }
            }
        }

        private async void OnApplicationQuit()
        {
            if (connection != null && isConnected)
            {
                try
                {
                    await connection.StopAsync();
                }
                catch { }
            }
        }
    }
}

