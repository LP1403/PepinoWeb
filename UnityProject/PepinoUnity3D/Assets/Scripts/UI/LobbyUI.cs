using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PepinoGame.Managers;

namespace PepinoGame.UI
{
    /// <summary>
    /// Lobby UI: auto-connects via NetworkManager; player only enters name + room and joins.
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

        private void Start()
        {
            if (joinButton != null)
                joinButton.onClick.AddListener(OnJoinButtonClicked);

            // Connect is automatic — hide legacy button if present
            if (connectButton != null)
            {
                connectButton.gameObject.SetActive(false);
            }

            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnConnectionChanged += OnConnectionChanged;
            }

            if (roomIdInput != null)
                roomIdInput.text = defaultRoomId;

            if (NetworkManager.Instance != null && NetworkManager.Instance.IsConnected)
            {
                UpdateStatusText("Conectado al servidor");
            }
            else
            {
                UpdateStatusText("Conectando al servidor...");
            }

            UpdateUI();
        }

        private async void OnJoinButtonClicked()
        {
            string roomId = roomIdInput?.text?.Trim() ?? "";
            string playerName = playerNameInput?.text?.Trim() ?? "";

            if (string.IsNullOrEmpty(roomId))
            {
                UpdateStatusText("Ingresa un ID de sala");
                return;
            }

            if (string.IsNullOrEmpty(playerName))
            {
                UpdateStatusText("Ingresa tu nombre");
                return;
            }

            if (playerName.Length < 3)
            {
                UpdateStatusText("El nombre debe tener al menos 3 caracteres");
                return;
            }

            if (NetworkManager.Instance == null || !NetworkManager.Instance.IsConnected)
            {
                UpdateStatusText("Aun conectando al servidor...");
                if (NetworkManager.Instance == null)
                {
                    UpdateStatusText("NetworkManager no encontrado en la escena");
                    return;
                }

                try
                {
                    await NetworkManager.Instance.ConnectToServer();
                }
                catch (System.Exception ex)
                {
                    UpdateStatusText($"No se pudo conectar: {ex.Message}");
                    return;
                }
            }

            try
            {
                UpdateStatusText($"Uniendose a sala {roomId}...");
                await NetworkManager.Instance.JoinRoom(roomId, playerName);
                GameManager.Instance.InitializeGame(roomId, playerName);
                UpdateStatusText($"Unido a sala {roomId}");
                ShowGamePanel();
            }
            catch (System.Exception ex)
            {
                UpdateStatusText($"Error: {ex.Message}");
            }
        }

        private void OnConnectionChanged(bool connected)
        {
            UpdateUI();
            UpdateStatusText(connected
                ? "Conectado al servidor — elige nombre y sala"
                : "Desconectado — reintentando...");
        }

        private void UpdateUI()
        {
            bool isConnected = NetworkManager.Instance?.IsConnected ?? false;

            if (joinButton != null)
                joinButton.interactable = true;

            if (roomIdInput != null)
                roomIdInput.interactable = true;

            if (playerNameInput != null)
                playerNameInput.interactable = true;

            if (!isConnected && statusText != null &&
                (statusText.text == null || !statusText.text.Contains("reintentando")))
            {
                // Keep connecting message unless already set by OnConnectionChanged
            }
        }

        private void UpdateStatusText(string message)
        {
            if (statusText != null)
                statusText.text = message;
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

            if (NetworkManager.Instance != null)
                NetworkManager.Instance.OnConnectionChanged -= OnConnectionChanged;
        }
    }
}
