using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PepinoGame.Managers;

namespace PepinoGame.UI
{
    /// <summary>
    /// Maneja la UI del lobby (unirse a sala)
    /// </summary>
    public class LobbyUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_InputField roomIdInput;
        [SerializeField] private TMP_InputField playerNameInput;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button connectButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private GameObject gamePanel;

        [Header("Settings")]
        [SerializeField] private string defaultRoomId = "SALA1";

        private bool isConnecting = false;

        private void Start()
        {
            // Configurar botones
            if (joinButton != null)
                joinButton.onClick.AddListener(OnJoinButtonClicked);
            
            if (connectButton != null)
                connectButton.onClick.AddListener(OnConnectButtonClicked);

            // Suscribirse a eventos del NetworkManager
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnConnectionChanged += OnConnectionChanged;
            }

            // Valores por defecto
            if (roomIdInput != null)
                roomIdInput.text = defaultRoomId;

            // Estado inicial
            UpdateUI();
        }

        private async void OnConnectButtonClicked()
        {
            if (isConnecting) return;
            
            isConnecting = true;
            UpdateStatusText("🔄 Conectando al servidor...");
            
            try
            {
                await NetworkManager.Instance.ConnectToServer();
                UpdateStatusText("✅ Conectado al servidor");
            }
            catch (System.Exception ex)
            {
                UpdateStatusText($"❌ Error: {ex.Message}");
                isConnecting = false;
            }
        }

        private async void OnJoinButtonClicked()
        {
            string roomId = roomIdInput?.text?.Trim() ?? "";
            string playerName = playerNameInput?.text?.Trim() ?? "";

            // Validaciones
            if (string.IsNullOrEmpty(roomId))
            {
                UpdateStatusText("⚠️ Ingresa un ID de sala");
                return;
            }

            if (string.IsNullOrEmpty(playerName))
            {
                UpdateStatusText("⚠️ Ingresa tu nombre");
                return;
            }

            if (playerName.Length < 3)
            {
                UpdateStatusText("⚠️ El nombre debe tener al menos 3 caracteres");
                return;
            }

            if (!NetworkManager.Instance.IsConnected)
            {
                UpdateStatusText("⚠️ No estás conectado al servidor");
                return;
            }

            try
            {
                UpdateStatusText($"🚪 Uniéndose a sala {roomId}...");
                
                // Unirse a la sala
                await NetworkManager.Instance.JoinRoom(roomId, playerName);
                
                // Inicializar el juego
                GameManager.Instance.InitializeGame(roomId, playerName);
                
                UpdateStatusText($"✅ Unido a sala {roomId}");
                
                // Cambiar a la escena del juego
                ShowGamePanel();
            }
            catch (System.Exception ex)
            {
                UpdateStatusText($"❌ Error: {ex.Message}");
            }
        }

        private void OnConnectionChanged(bool connected)
        {
            isConnecting = false;
            UpdateUI();
            
            if (connected)
            {
                UpdateStatusText("✅ Conectado al servidor");
            }
            else
            {
                UpdateStatusText("❌ Desconectado del servidor");
            }
        }

        private void UpdateUI()
        {
            bool isConnected = NetworkManager.Instance?.IsConnected ?? false;
            
            if (connectButton != null)
                connectButton.interactable = !isConnected && !isConnecting;
            
            if (joinButton != null)
                joinButton.interactable = isConnected;
            
            if (roomIdInput != null)
                roomIdInput.interactable = isConnected;
            
            if (playerNameInput != null)
                playerNameInput.interactable = isConnected;
        }

        private void UpdateStatusText(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            Debug.Log($"[LobbyUI] {message}");
        }

        private void ShowGamePanel()
        {
            if (lobbyPanel != null)
                lobbyPanel.SetActive(false);
            
            if (gamePanel != null)
                gamePanel.SetActive(true);
        }

        private void OnDestroy()
        {
            if (joinButton != null)
                joinButton.onClick.RemoveListener(OnJoinButtonClicked);
            
            if (connectButton != null)
                connectButton.onClick.RemoveListener(OnConnectButtonClicked);
            
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnConnectionChanged -= OnConnectionChanged;
            }
        }
    }
}

