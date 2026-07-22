using UnityEngine;
using PepinoGame.Controllers;
using PepinoGame.Config;

namespace PepinoGame.Utils
{
    /// <summary>
    /// UNO-like framing: close on the table, hand locked to the camera (bottom of view),
    /// opponents show a short fan of backs on the far rim.
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
            if (Object.FindAnyObjectByType<PepinoAlphaBootstrap>() != null)
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
            EnsureCardTable(tablePrefab);

            var table = EnsureEmpty("TableContainer", new Vector3(0f, 0.88f, 0.05f));
            var hand = EnsureEmpty("HandContainer", Vector3.zero);

            Object.FindAnyObjectByType<HandManager>()?.Configure(hand.transform, fallback, config);
            Object.FindAnyObjectByType<TableManager>()?.Configure(table.transform, fallback);

            EnsureOpponentSeats();
            FrameUnoView(hand.transform);
            StyleEnvironment();
            HideConnectButton();
        }

        private static void EnsureResolver(GameObject fallback)
        {
            var existing = Object.FindAnyObjectByType<CardVisualResolver>();
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
            var manager = Object.FindAnyObjectByType<OpponentSeatManager>();
            if (manager == null)
            {
                var go = new GameObject("OpponentSeatManager");
                manager = go.AddComponent<OpponentSeatManager>();
            }

            manager.Configure(tableRadius: 1.35f, seatHeight: 0.95f, cardScale: 2.6f);
        }

        private static GameObject EnsureEmpty(string name, Vector3 position)
        {
            var go = GameObject.Find(name);
            if (go == null)
                go = new GameObject(name);

            go.transform.SetParent(null, true);
            go.transform.position = position;
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go;
        }

        private static void EnsureCardTable(GameObject prefab)
        {
            var table = GameObject.Find("CardTable");
            if (table == null && prefab != null)
            {
                table = Instantiate(prefab);
                table.name = "CardTable";
            }

            if (table == null) return;

            table.transform.SetParent(null);
            table.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            table.transform.localScale = Vector3.one;
        }

        private static void FrameUnoView(Transform hand)
        {
            var cam = Camera.main;
            if (cam == null) return;

            cam.transform.SetParent(null);
            // Close seated angle — table fills most of the Game view
            cam.transform.position = new Vector3(0f, 2.45f, -2.15f);
            cam.transform.rotation = Quaternion.Euler(52f, 0f, 0f);
            cam.fieldOfView = 52f;
            cam.nearClipPlane = 0.08f;
            cam.farClipPlane = 30f;

            if (hand == null) return;

            // Hand glued to the lens: always large and readable at the bottom (UNO)
            hand.SetParent(cam.transform, false);
            hand.localPosition = new Vector3(0f, -0.78f, 1.05f);
            hand.localRotation = Quaternion.identity;
            hand.localScale = Vector3.one;
        }

        private static void StyleEnvironment()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.14f, 0.18f, 0.24f, 1f);
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.62f);

            var light = Object.FindAnyObjectByType<Light>();
            if (light != null && light.type == LightType.Directional)
            {
                light.transform.rotation = Quaternion.Euler(48f, -20f, 0f);
                light.intensity = 1.2f;
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
