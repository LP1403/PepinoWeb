using UnityEngine;

namespace PepinoGame.Config
{
    /// <summary>
    /// Configuración global del juego
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Pepino/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Network Settings")]
        [Tooltip("URL del servidor SignalR (tu backend .NET)")]
        public string serverUrl = "http://localhost:5264/gamehub";
        
        [Tooltip("Tiempo de reconexión automática (segundos)")]
        public float[] reconnectionDelays = new float[] { 0f, 2f, 10f, 30f };

        [Header("Game Settings")]
        [Tooltip("Máximo de jugadores por sala")]
        public int maxPlayersPerRoom = 8;
        
        [Tooltip("Mínimo de jugadores para iniciar")]
        public int minPlayersToStart = 2;

        [Header("UI Settings")]
        [Tooltip("Duración de las animaciones de cartas (segundos)")]
        public float cardAnimationDuration = 0.3f;
        
        [Tooltip("Duración del efecto PEPINEADO (segundos)")]
        public float pepineadoEffectDuration = 3f;
        
        [Tooltip("Escala de carta seleccionada")]
        public float selectedCardScale = 1.2f;

        [Header("3D Settings")]
        [Tooltip("Distancia entre cartas en la mano (pack scale)")]
        public float cardSpacing = 0.28f;
        
        [Tooltip("Radio del arco de la mano (pack scale)")]
        public float handArcRadius = 3.4f;
        
        [Tooltip("Altura de elevación al seleccionar carta")]
        public float selectedCardHeight = 0.5f;

        [Header("Debug")]
        [Tooltip("Activar logs detallados")]
        public bool enableDebugLogs = true;
    }
}

