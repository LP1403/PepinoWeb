using UnityEngine;
using PepinoGame.Controllers;
using PepinoGame.Config;

namespace PepinoGame.Utils
{
    /// <summary>
    /// Runtime presentation: seated POV with full table + far opponent + hand band.
    /// Felt height is measured from the CardTable mesh so cards/rivals never spawn inside wood.
    /// </summary>
    public class PepinoAlphaBootstrap : MonoBehaviour
    {
        private const string TablePrefabPath =
            "Assets/SubstanceAssets/LittleGamesPack/Prefabs/Individual Pieces/Cards/CardTable.prefab";
        private const string FallbackCardPath = "Assets/CardPrefab.prefab";
        private const string ConfigPath = "Assets/GameConfig.asset";
        private const string ChipPrefabPath =
            "Assets/SubstanceAssets/LittleGamesPack/Prefabs/Individual Pieces/PokerChips/PokerChip_Red.prefab";
        private const string DicePrefabPath =
            "Assets/SubstanceAssets/LittleGamesPack/Prefabs/Individual Pieces/Dice/Dice_White.prefab";

        /// <summary>World Y of the playable felt surface (above mesh top).</summary>
        public static float FeltY { get; private set; } = 0.9f;

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
            GameObject chipPrefab = null;
            GameObject dicePrefab = null;

#if UNITY_EDITOR
            fallback = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(FallbackCardPath);
            config = UnityEditor.AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            tablePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(TablePrefabPath);
            chipPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(ChipPrefabPath);
            dicePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(DicePrefabPath);
#endif

            EnsureResolver(fallback);
            EnsureCardTable(tablePrefab);
            FeltY = MeasureFeltTop();

            var table = EnsureEmpty("TableContainer", new Vector3(0f, FeltY, 0f));
            var hand = EnsureEmpty("HandContainer", Vector3.zero);

            Object.FindAnyObjectByType<HandManager>()?.Configure(hand.transform, fallback, config);
            var tableManager = Object.FindAnyObjectByType<TableManager>();
            tableManager?.Configure(
                table.transform,
                fallback,
                // Slightly toward the player so discard reads large in the POV
                tableCenter: new Vector3(0.05f, 0.06f, -0.12f),
                drawPileOffset: new Vector3(-0.42f, 0.04f, 0.02f));
            tableManager?.EnsureDecorativeDeck();

            EnsureOpponentSeats();
            EnsureTableProps(chipPrefab, dicePrefab);
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

        /// <summary>Top of CardTable mesh + small clearance so nothing clips into wood/felt.</summary>
        public static float MeasureFeltTop()
        {
            var table = GameObject.Find("CardTable");
            if (table == null) return FeltY;

            var rs = table.GetComponentsInChildren<Renderer>();
            if (rs == null || rs.Length == 0) return FeltY;

            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++)
                b.Encapsulate(rs[i].bounds);

            return b.max.y + 0.04f;
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

            // Closer rim + above felt so fan sits mid-upper frame, not clipped / off-screen
            manager.Configure(tableRadius: 0.55f, seatHeight: FeltY + 0.25f, cardScale: 5.5f);
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
            table.transform.localScale = Vector3.one * 1.2f;
        }

        private static void EnsureTableProps(GameObject chipPrefab, GameObject dicePrefab)
        {
            var existing = GameObject.Find("TableProps");
            if (existing != null)
                Object.Destroy(existing);

            var root = new GameObject("TableProps");
            root.transform.position = Vector3.zero;

            if (chipPrefab != null)
            {
                PlaceProp(chipPrefab, root.transform, new Vector3(-0.78f, FeltY + 0.04f, 0.45f), 0.28f);
                PlaceProp(chipPrefab, root.transform, new Vector3(-0.72f, FeltY + 0.06f, 0.5f), 0.28f);
            }

            if (dicePrefab != null)
                PlaceProp(dicePrefab, root.transform, new Vector3(0.78f, FeltY + 0.04f, 0.35f), 0.35f);
        }

        private static void PlaceProp(GameObject prefab, Transform parent, Vector3 pos, float scale)
        {
            var go = Object.Instantiate(prefab, parent);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * scale;
            foreach (var rb in go.GetComponentsInChildren<Rigidbody>())
            {
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }
        }

        /// <summary>
        /// Seated POV: hand at bottom, discard mid-table, rivals mid-upper — all above felt.
        /// </summary>
        public static void ApplyCameraFrame()
        {
            var cam = Camera.main;
            if (cam == null) return;

            float felt = FeltY > 0.1f ? FeltY : MeasureFeltTop();
            cam.transform.SetParent(null);
            Vector3 focus = new Vector3(0f, felt + 0.05f, 0.05f);
            cam.transform.position = new Vector3(0f, felt + 1.05f, -2.55f);
            cam.transform.LookAt(focus);
            cam.fieldOfView = 48f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 40f;
        }

        private static void StyleEnvironment()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.04f, 0.045f, 0.055f, 1f);
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.4f, 0.38f);

            var keyGo = GameObject.Find("TableKeyLight");
            if (keyGo != null)
                Object.Destroy(keyGo);

            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
            {
                if (light.type != LightType.Directional) continue;
                light.transform.rotation = Quaternion.Euler(48f, -25f, 0f);
                light.intensity = 0.95f;
                light.color = new Color(1f, 0.96f, 0.9f);
                light.shadows = LightShadows.Soft;
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
