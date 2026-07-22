using UnityEngine;
using PepinoGame.Controllers;
using PepinoGame.Config;

namespace PepinoGame.Utils
{
    /// <summary>
    /// UNO-like POV: table in the upper area, hand always closest to camera at the bottom.
    /// </summary>
    public class PepinoAlphaBootstrap : MonoBehaviour
    {
        private const string TablePrefabPath =
            "Assets/SubstanceAssets/LittleGamesPack/Prefabs/Individual Pieces/Cards/CardTable.prefab";
        private const string FallbackCardPath = "Assets/CardPrefab.prefab";
        private const string ConfigPath = "Assets/GameConfig.asset";

        private bool framed;

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

            var table = EnsureEmpty("TableContainer", new Vector3(0f, 0.88f, 0.15f));
            var hand = EnsureEmpty("HandContainer", Vector3.zero);

            Object.FindAnyObjectByType<HandManager>()?.Configure(hand.transform, fallback, config);
            Object.FindAnyObjectByType<TableManager>()?.Configure(table.transform, fallback);

            EnsureOpponentSeats();
            ApplyCameraFrame();
            StyleEnvironment();
            HideConnectButton();
            framed = true;
        }

        private void LateUpdate()
        {
            if (!framed) return;
            ApplyCameraFrame();
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

            manager.Configure(tableRadius: 1.05f, seatHeight: 0.92f, cardScale: 2.0f);
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
            // Mesa cerca de la cámara: ocupa el centro-arriba (POV sentado estilo UNO)
            table.transform.SetPositionAndRotation(new Vector3(0f, 0f, 0.2f), Quaternion.identity);
            table.transform.localScale = Vector3.one;
        }

        private static void ApplyCameraFrame()
        {
            var cam = Camera.main;
            if (cam == null) return;

            cam.transform.SetParent(null);
            cam.transform.position = new Vector3(0f, 1.95f, -1.65f);
            cam.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
            cam.fieldOfView = 52f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 30f;
        }

        private static void StyleEnvironment()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.12f, 0.16f, 0.22f, 1f);
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.62f);

            var light = Object.FindAnyObjectByType<Light>();
            if (light != null && light.type == LightType.Directional)
            {
                light.transform.rotation = Quaternion.Euler(50f, -20f, 0f);
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
