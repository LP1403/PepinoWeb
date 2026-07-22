using UnityEngine;
using PepinoGame.Controllers;
using PepinoGame.Config;

namespace PepinoGame.Utils
{
    /// <summary>
    /// Ensures alpha scene objects exist at runtime even if Editor setup was not run.
    /// Frames the table like a seated card game (UNO-style overhead from the south seat).
    /// </summary>
    public class PepinoAlphaBootstrap : MonoBehaviour
    {
        private const string TablePrefabPath =
            "Assets/SubstanceAssets/LittleGamesPack/Prefabs/Individual Pieces/Cards/CardTable.prefab";
        private const string FallbackCardPath = "Assets/CardPrefab.prefab";
        private const string ConfigPath = "Assets/GameConfig.asset";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterSceneLoad()
        {
            if (Object.FindFirstObjectByType<PepinoAlphaBootstrap>() != null)
                return;

            var go = new GameObject("PepinoAlphaBootstrap");
            go.AddComponent<PepinoAlphaBootstrap>();
        }

        private void Awake()
        {
            GameObject fallback = null;
            GameConfig config = null;
            GameObject tablePrefab = null;

#if UNITY_EDITOR
            fallback = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(FallbackCardPath);
            config = UnityEditor.AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            tablePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(TablePrefabPath);
#endif

            EnsureResolver(fallback);
            // Hand sits near the south edge — readable in the lower third of the Game view
            var hand = EnsureEmpty("HandContainer", new Vector3(0f, 0.55f, -2.55f));
            var table = EnsureEmpty("TableContainer", new Vector3(0f, 0.92f, 0.05f));
            EnsureCardTable(tablePrefab);

            var handManager = Object.FindFirstObjectByType<HandManager>();
            handManager?.Configure(hand.transform, fallback, config);

            var tableManager = Object.FindFirstObjectByType<TableManager>();
            tableManager?.Configure(table.transform, fallback);

            EnsureOpponentSeats();
            PositionCamera();
            StyleEnvironment();
            HideConnectButton();
        }

        private static void EnsureResolver(GameObject fallback)
        {
            var existing = Object.FindFirstObjectByType<CardVisualResolver>();
            if (existing != null)
            {
                if (fallback != null) existing.SetFallbackPrefab(fallback);
                return;
            }

            var go = new GameObject("CardVisualResolver");
            var resolver = go.AddComponent<CardVisualResolver>();
            if (fallback != null) resolver.SetFallbackPrefab(fallback);
        }

        private static void EnsureOpponentSeats()
        {
            if (Object.FindFirstObjectByType<OpponentSeatManager>() != null)
                return;

            var go = new GameObject("OpponentSeatManager");
            go.AddComponent<OpponentSeatManager>();
        }

        private static GameObject EnsureEmpty(string name, Vector3 position)
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                go = new GameObject(name);
            }

            go.transform.position = position;
            return go;
        }

        private static void EnsureCardTable(GameObject prefab)
        {
            if (GameObject.Find("CardTable") != null || prefab == null) return;

            var table = Instantiate(prefab);
            table.name = "CardTable";
            table.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        private static void PositionCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;

            // Seated at the south edge, looking over your hand onto the table (UNO framing)
            cam.transform.position = new Vector3(0f, 5.2f, -5.4f);
            cam.transform.rotation = Quaternion.Euler(52f, 0f, 0f);
            cam.fieldOfView = 42f;
            cam.nearClipPlane = 0.1f;
        }

        private static void StyleEnvironment()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.35f, 0.62f, 0.85f, 1f); // soft sky blue
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.65f, 0.7f, 0.75f);

            var light = Object.FindFirstObjectByType<Light>();
            if (light != null && light.type == LightType.Directional)
            {
                light.transform.rotation = Quaternion.Euler(48f, -25f, 0f);
                light.intensity = 1.15f;
                light.color = new Color(1f, 0.98f, 0.94f);
            }
        }

        private static void HideConnectButton()
        {
            var btn = GameObject.Find("ConnectButton");
            if (btn != null)
                btn.SetActive(false);
        }
    }
}
