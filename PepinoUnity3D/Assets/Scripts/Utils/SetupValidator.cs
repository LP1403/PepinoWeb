using UnityEngine;
using PepinoGame.Managers;
using PepinoGame.Config;

namespace PepinoGame.Utils
{
    /// <summary>
    /// Valida que el proyecto esté configurado correctamente
    /// Útil para debugging y setup inicial
    /// </summary>
    public class SetupValidator : MonoBehaviour
    {
        [Header("Auto-Check on Start")]
        [SerializeField] private bool validateOnStart = true;

        private void Start()
        {
            if (validateOnStart)
            {
                ValidateSetup();
            }
        }

        [ContextMenu("Validate Setup")]
        public void ValidateSetup()
        {
            Debug.Log("=== 🔍 VALIDANDO CONFIGURACIÓN DEL PROYECTO ===");
            
            bool allValid = true;

            // 1. Verificar NetworkManager
            allValid &= ValidateNetworkManager();
            
            // 2. Verificar GameManager
            allValid &= ValidateGameManager();
            
            // 3. Verificar GameConfig
            allValid &= ValidateGameConfig();

            // 4. Verificar Dependencias
            allValid &= ValidateDependencies();

            // Resultado final
            Debug.Log("========================================");
            if (allValid)
            {
                Debug.Log("✅ CONFIGURACIÓN VÁLIDA - ¡Listo para jugar!");
            }
            else
            {
                Debug.LogError("❌ CONFIGURACIÓN INCOMPLETA - Revisa los errores arriba");
            }
            Debug.Log("========================================");
        }

        private bool ValidateNetworkManager()
        {
            Debug.Log("\n📡 Validando NetworkManager...");
            
            if (NetworkManager.Instance == null)
            {
                Debug.LogError("❌ NetworkManager no encontrado en la escena");
                Debug.Log("   → Crea un GameObject y añade el script NetworkManager.cs");
                return false;
            }

            Debug.Log("✅ NetworkManager encontrado");

            // Verificar GameConfig asignado
            var configField = NetworkManager.Instance.GetType()
                .GetField("gameConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (configField != null)
            {
                var config = configField.GetValue(NetworkManager.Instance) as GameConfig;
                if (config == null)
                {
                    Debug.LogWarning("⚠️ GameConfig no asignado en NetworkManager");
                    Debug.Log("   → Asigna un GameConfig ScriptableObject");
                    return false;
                }
                else
                {
                    Debug.Log($"✅ GameConfig asignado: {config.name}");
                }
            }

            return true;
        }

        private bool ValidateGameManager()
        {
            Debug.Log("\n🎮 Validando GameManager...");
            
            if (GameManager.Instance == null)
            {
                Debug.LogError("❌ GameManager no encontrado en la escena");
                Debug.Log("   → Crea un GameObject y añade el script GameManager.cs");
                return false;
            }

            Debug.Log("✅ GameManager encontrado");
            return true;
        }

        private bool ValidateGameConfig()
        {
            Debug.Log("\n⚙️ Validando GameConfig...");
            
            GameConfig[] configs = Resources.FindObjectsOfTypeAll<GameConfig>();
            
            if (configs.Length == 0)
            {
                Debug.LogError("❌ No se encontró ningún GameConfig");
                Debug.Log("   → Right-click > Create > Pepino > GameConfig");
                return false;
            }

            Debug.Log($"✅ GameConfig(s) encontrado(s): {configs.Length}");
            
            foreach (var config in configs)
            {
                Debug.Log($"   📋 {config.name}:");
                Debug.Log($"      Server URL: {config.serverUrl}");
                Debug.Log($"      Debug Logs: {config.enableDebugLogs}");
            }

            return true;
        }

        private bool ValidateDependencies()
        {
            Debug.Log("\n📦 Validando Dependencias...");
            
            bool allValid = true;

            // TextMeshPro
            try
            {
                var tmp = typeof(TMPro.TextMeshProUGUI);
                Debug.Log("✅ TextMeshPro encontrado");
            }
            catch
            {
                Debug.LogError("❌ TextMeshPro no encontrado");
                Debug.Log("   → Window > TextMeshPro > Import TMP Essential Resources");
                allValid = false;
            }

            // SignalR
            try
            {
                var signalr = typeof(Microsoft.AspNetCore.SignalR.Client.HubConnection);
                Debug.Log("✅ SignalR Client encontrado");
            }
            catch
            {
                Debug.LogError("❌ SignalR Client no encontrado");
                Debug.Log("   → Instala Microsoft.AspNetCore.SignalR.Client desde NuGet");
                allValid = false;
            }

            // LeanTween (opcional pero recomendado)
            var leanTweenType = System.Type.GetType("LeanTween");
            if (leanTweenType == null)
            {
                Debug.LogWarning("⚠️ LeanTween no encontrado (opcional)");
                Debug.Log("   → Descarga desde: https://assetstore.unity.com/packages/tools/animation/leantween-3595");
            }
            else
            {
                Debug.Log("✅ LeanTween encontrado");
            }

            return allValid;
        }

        [ContextMenu("Test Backend Connection")]
        public async void TestBackendConnection()
        {
            Debug.Log("=== 🔌 PROBANDO CONEXIÓN AL BACKEND ===");
            
            if (NetworkManager.Instance == null)
            {
                Debug.LogError("❌ NetworkManager no encontrado");
                return;
            }

            try
            {
                Debug.Log("🔄 Intentando conectar...");
                await NetworkManager.Instance.ConnectToServer();
                Debug.Log("✅ ¡CONEXIÓN EXITOSA!");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ ERROR AL CONECTAR: {ex.Message}");
                Debug.Log("\n💡 Posibles soluciones:");
                Debug.Log("   1. Asegúrate que el backend esté corriendo:");
                Debug.Log("      cd Back/GameServer/GameServer");
                Debug.Log("      dotnet run");
                Debug.Log("   2. Verifica la URL en GameConfig (debe ser http://localhost:5264/gamehub)");
                Debug.Log("   3. Desactiva firewall/antivirus temporalmente");
            }
        }

        [ContextMenu("Show System Info")]
        public void ShowSystemInfo()
        {
            Debug.Log("=== 💻 INFORMACIÓN DEL SISTEMA ===");
            Debug.Log($"Unity Version: {Application.unityVersion}");
            Debug.Log($"Platform: {Application.platform}");
            Debug.Log($"Data Path: {Application.dataPath}");
            Debug.Log($"Persistent Data: {Application.persistentDataPath}");
            Debug.Log($"System Language: {Application.systemLanguage}");
            
            #if UNITY_EDITOR
            Debug.Log("Running in EDITOR mode");
            #else
            Debug.Log("Running in BUILD mode");
            #endif
        }
    }
}

