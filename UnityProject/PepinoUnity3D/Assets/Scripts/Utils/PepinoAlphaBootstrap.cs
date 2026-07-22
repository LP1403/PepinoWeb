using UnityEngine;
using PepinoGame.Controllers;
using PepinoGame.Config;

namespace PepinoGame.Utils
{
    /// <summary>
    /// Frames the 3D table like a seated card game:
    /// camera over the south seat, hand parented to camera, opponents around the rim.
    /// </summary>
    public class PepinoAlphaBootstrap : MonoBehaviour
    {
        private const string TablePrefabPath =
            "Assets/SubstanceAssets/LittleGamesPack/Prefabs/Individual Pieces/Cards/CardTable.prefab";
        private const string FallbackCardPath = "Assets/CardPrefab.prefab";
        private const string ConfigPath = "Assets/GameConfig.asset";

        // Pack cards are ~0.13m wide — scale the whole play surface up for readability
        private const float TableScale = 2.6f;

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
            EnsureCardTable(tablePrefab);

            var table = EnsureEmpty("TableContainer", new Vector3(0f, 0.95f * TableScale * 0.35f, 0.05f));
            // Hand starts in world; AttachHandToCamera reparents it
            var hand = EnsureEmpty("HandContainer", Vector3.zero);

            var handManager = Object.FindFirstObjectByType<HandManager>();
            handManager?.Configure(hand.transform, fallback, config);

            var tableManager = Object.FindFirstObjectByType<TableManager>();
            tableManager?.Configure(table.transform, fallback);

            EnsureOpponentSeats();
            FrameSeatedView(hand.transform);
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
            var manager = Object.FindFirstObjectByType<OpponentSeatManager>();
            if (manager == null)
            {
                var go = new GameObject("OpponentSeatManager");
                manager = go.AddComponent<OpponentSeatManager>();
            }

            manager.Configure(tableRadius: 1.55f * TableScale, seatHeight: 0.55f * TableScale, cardScale: 2.2f);
        }

        private static GameObject EnsureEmpty(string name, Vector3 position)
        {
            var go = GameObject.Find(name);
            if (go == null)
                go = new GameObject(name);

            go.transform.SetParent(null);
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

            table.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            table.transform.localScale = Vector3.one * TableScale;
        }

        /// <summary>
        /// Camera sits at the south edge looking at the table center;
        /// the local hand is locked to the bottom of the view (child of camera).
        /// </summary>
        private static void FrameSeatedView(Transform hand)
        {
            var cam = Camera.main;
            if (cam == null) return;

            // Close enough that the felt fills most of the Game view
            Vector3 lookTarget = new Vector3(0f, 1.0f, 0.2f);
            cam.transform.position = new Vector3(0f, 3.15f, -3.35f);
            cam.transform.LookAt(lookTarget);
            cam.fieldOfView = 48f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 50f;

            if (hand != null)
            {
                hand.SetParent(cam.transform, false);
                // Lower third of the screen, in front of the lens
                hand.localPosition = new Vector3(0f, -0.62f, 1.05f);
                hand.localRotation = Quaternion.identity;
                hand.localScale = Vector3.one;
            }
        }

        private static void StyleEnvironment()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.18f, 0.22f, 0.28f, 1f); // dark neutral, not toy-blue
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.62f);

            var light = Object.FindFirstObjectByType<Light>();
            if (light != null && light.type == LightType.Directional)
            {
                light.transform.rotation = Quaternion.Euler(42f, -20f, 0f);
                light.intensity = 1.2f;
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
