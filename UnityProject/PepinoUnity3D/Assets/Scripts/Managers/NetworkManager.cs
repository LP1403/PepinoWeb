using System;
using System.Collections.Generic;
using System.Text.Json;
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

        private bool isConnecting;
        private bool autoConnectStarted;

        private readonly Queue<Action> mainThreadQueue = new Queue<Action>();
        private readonly object mainThreadLock = new object();

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

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
        }

        private void Start()
        {
            if (!autoConnectStarted)
            {
                autoConnectStarted = true;
                _ = AutoConnectLoop();
            }
        }

        private void Update()
        {
            FlushMainThreadQueue();
        }

        /// <summary>
        /// Keeps trying to connect until the hub is reachable (alpha UX: no Connect button).
        /// </summary>
        private async Task AutoConnectLoop()
        {
            while (this != null && Application.isPlaying)
            {
                if (isConnected)
                {
                    await Task.Delay(2000);
                    continue;
                }

                try
                {
                    await ConnectToServer();
                }
                catch
                {
                    // Retry below
                }

                if (!isConnected)
                    await Task.Delay(3000);
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
                isConnected = true;
                RunOnMainThread(() => OnConnectionChanged?.Invoke(true));
                return;
            }

            if (isConnecting) return;
            isConnecting = true;

            try
            {
                if (gameConfig == null)
                {
                    throw new Exception("GameConfig no asignado en NetworkManager");
                }

                Log($"🔄 Conectando a SignalR: {gameConfig.serverUrl}");

                if (connection != null)
                {
                    try
                    {
                        await connection.DisposeAsync();
                    }
                    catch { }
                    connection = null;
                }

                connection = new HubConnectionBuilder()
                    .WithUrl(gameConfig.serverUrl)
                    .WithAutomaticReconnect(GetReconnectDelays())
                    .Build();

                SetupHubEvents();

                await connection.StartAsync();

                myConnectionId = connection.ConnectionId;
                isConnected = true;

                Log($"✅ Conectado exitosamente! ConnectionId: {myConnectionId}");
                RunOnMainThread(() => OnConnectionChanged?.Invoke(true));
            }
            catch (Exception ex)
            {
                LogError($"❌ Error al conectar: {ex.Message}");
                isConnected = false;
                RunOnMainThread(() => OnConnectionChanged?.Invoke(false));
                throw;
            }
            finally
            {
                isConnecting = false;
            }
        }

        /// <summary>
        /// Configura todos los eventos que vienen del servidor.
        /// Deserializa via JsonElement + case-insensitive para no perder estado cuando llegan cartas.
        /// Encola al hilo principal: SignalR corre en thread pool y Unity UI/API no es thread-safe.
        /// </summary>
        private void SetupHubEvents()
        {
            connection.On<JsonElement>("GameStateUpdated", (element) =>
            {
                RunOnMainThread(() =>
                {
                    try
                    {
                        var gameState = DeserializePayload<GameState>(element);
                        if (gameState == null)
                        {
                            LogError("GameStateUpdated llegó null / no deserializable");
                            return;
                        }

                        Log($"🔄 Estado del juego actualizado");
                        Log($"DEBUG RAW - RoomId: {gameState.roomId}");
                        Log($"DEBUG RAW - isRoomCreator: {gameState.isRoomCreator}");
                        Log($"DEBUG RAW - isGameStarted: {gameState.isGameStarted}");
                        Log($"DEBUG RAW - players COUNT: {gameState.players?.Count ?? -1}");
                        Log($"DEBUG RAW - yourHand COUNT: {gameState.yourHand?.Count ?? -1}");

                        OnGameStateUpdated?.Invoke(gameState);
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error procesando GameStateUpdated: {ex.Message}\n{ex.StackTrace}");
                    }
                });
            });

            connection.On<JsonElement>("CardsDealt", (element) =>
            {
                RunOnMainThread(() =>
                {
                    try
                    {
                        var hand = DeserializePayload<List<Card>>(element) ?? new List<Card>();
                        Log($"🎴 Cartas recibidas: {hand.Count}");
                        OnCardsDealt?.Invoke(hand);
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error procesando CardsDealt: {ex.Message}");
                    }
                });
            });

            connection.On<JsonElement>("CardsPlayed", (element) =>
            {
                RunOnMainThread(() =>
                {
                    try
                    {
                        var playedCards = DeserializePayload<PlayedCards>(element);
                        if (playedCards == null) return;
                        Log($"🃏 {playedCards.playerName} jugó {playedCards.cards?.Count ?? 0} carta(s)");
                        OnCardsPlayed?.Invoke(playedCards);
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error procesando CardsPlayed: {ex.Message}");
                    }
                });
            });

            connection.On<string, int>("PlayerJoined", (playerName, playerCount) =>
            {
                RunOnMainThread(() =>
                {
                    Log($"👤 {playerName} se unió. Total: {playerCount}");
                    OnPlayerJoined?.Invoke(playerName);
                });
            });

            connection.On<string, int>("PlayerLeft", (playerName, playerCount) =>
            {
                RunOnMainThread(() =>
                {
                    Log($"👋 {playerName} se fue. Quedan: {playerCount}");
                    OnPlayerLeft?.Invoke(playerName);
                });
            });

            connection.On<string>("PlayerWon", (playerName) =>
            {
                RunOnMainThread(() =>
                {
                    Log($"🏆 {playerName} ganó!");
                    OnPlayerWon?.Invoke(playerName);
                });
            });

            connection.On<string>("PlayerSkipped", (playerName) =>
            {
                RunOnMainThread(() =>
                {
                    Log($"⏭️ {playerName} fue saltado!");
                    OnPlayerSkipped?.Invoke(playerName);
                });
            });

            connection.On<string>("GameStarted", (roomId) =>
            {
                RunOnMainThread(() =>
                {
                    Log($"🎮 Juego iniciado en sala {roomId}");
                    OnGameStarted?.Invoke(roomId);
                });
            });

            connection.On<string>("Error", (message) =>
            {
                RunOnMainThread(() =>
                {
                    LogError($"❌ Error del servidor: {message}");
                    OnError?.Invoke(message);
                });
            });

            connection.Reconnecting += (error) =>
            {
                Log("🔄 Reconectando...");
                isConnected = false;
                RunOnMainThread(() => OnConnectionChanged?.Invoke(false));
                return Task.CompletedTask;
            };

            connection.Reconnected += (connectionId) =>
            {
                Log($"✅ Reconectado! ConnectionId: {connectionId}");
                myConnectionId = connectionId;
                isConnected = true;
                RunOnMainThread(() => OnConnectionChanged?.Invoke(true));
                return Task.CompletedTask;
            };

            connection.Closed += (error) =>
            {
                Log("🔌 Conexión cerrada");
                isConnected = false;
                RunOnMainThread(() => OnConnectionChanged?.Invoke(false));
                return Task.CompletedTask;
            };
        }

        #region Server Invocations (Llamadas al servidor)

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

        public async Task SelectGameMode(string roomId, int deckCount)
        {
            if (!isConnected) return;

            if (string.IsNullOrWhiteSpace(roomId))
                throw new Exception("roomId vacío al seleccionar modo");

            try
            {
                Log($"🎯 Seleccionando modo: {deckCount} mazos (sala {roomId})");
                await connection.InvokeAsync("SelectGameMode", roomId, deckCount);
            }
            catch (Exception ex)
            {
                LogError($"Error al seleccionar modo: {ex.Message}");
                throw;
            }
        }

        public async Task StartGame(string roomId)
        {
            if (!isConnected) return;

            if (string.IsNullOrWhiteSpace(roomId))
                throw new Exception("roomId vacío al iniciar partida");

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

        private void RunOnMainThread(Action action)
        {
            if (action == null) return;
            lock (mainThreadLock)
            {
                mainThreadQueue.Enqueue(action);
            }
        }

        private void FlushMainThreadQueue()
        {
            while (true)
            {
                Action action = null;
                lock (mainThreadLock)
                {
                    if (mainThreadQueue.Count == 0) break;
                    action = mainThreadQueue.Dequeue();
                }

                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    LogError($"Error en callback main-thread: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        private static T DeserializePayload<T>(JsonElement element)
        {
            return JsonSerializer.Deserialize<T>(element.GetRawText(), JsonOptions);
        }

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
            if (gameConfig != null && gameConfig.enableDebugLogs)
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
            if (Instance == this)
                Instance = null;

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
