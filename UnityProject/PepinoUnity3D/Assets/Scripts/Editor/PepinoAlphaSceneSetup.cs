using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
using PepinoGame.Controllers;
using PepinoGame.Managers;
using PepinoGame.UI;
using PepinoGame.Utils;
using PepinoGame.Config;

namespace PepinoGame.EditorTools
{
    /// <summary>
    /// One-shot alpha scene wiring (menu + auto-run once if GameScene is open).
    /// </summary>
    public static class PepinoAlphaSceneSetup
    {
        private const string ScenePath = "Assets/GameScene.unity";
        private const string TablePrefabPath =
            "Assets/SubstanceAssets/LittleGamesPack/Prefabs/Individual Pieces/Cards/CardTable.prefab";
        private const string FallbackCardPath = "Assets/CardPrefab.prefab";
        private const string ConfigPath = "Assets/GameConfig.asset";

        [MenuItem("Pepino/Setup Alpha Scene")]
        public static void SetupFromMenu()
        {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath);

            SetupScene();
            EditorUtility.DisplayDialog("Pepino", "Alpha scene setup complete.", "OK");
        }

        [InitializeOnLoadMethod]
        private static void AutoSetupIfNeeded()
        {
            EditorApplication.delayCall += () =>
            {
                if (Application.isPlaying) return;
                var scene = EditorSceneManager.GetActiveScene();
                if (scene.path != ScenePath) return;
                if (GameObject.Find("CardVisualResolver") != null && GameObject.Find("CardTable") != null)
                    return;
                SetupScene();
                Debug.Log("[PepinoAlphaSceneSetup] Auto-wired GameScene for alpha.");
            };
        }

        public static void SetupScene()
        {
            var handGo = FindOrCreate("HandContainer");
            handGo.transform.position = new Vector3(0f, 0.55f, -2.55f);

            var tableGo = FindOrCreate("TableContainer");
            tableGo.transform.position = new Vector3(0f, 0.92f, 0.05f);

            var tablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TablePrefabPath);
            var cardTable = GameObject.Find("CardTable");
            if (cardTable == null && tablePrefab != null)
            {
                cardTable = (GameObject)PrefabUtility.InstantiatePrefab(tablePrefab);
                cardTable.name = "CardTable";
                Undo.RegisterCreatedObjectUndo(cardTable, "Create CardTable");
            }

            if (cardTable != null)
            {
                cardTable.transform.position = Vector3.zero;
                cardTable.transform.rotation = Quaternion.identity;
                cardTable.transform.localScale = Vector3.one;
            }

            var resolverGo = FindOrCreate("CardVisualResolver");
            var resolver = resolverGo.GetComponent<CardVisualResolver>() ??
                           Undo.AddComponent<CardVisualResolver>(resolverGo);
            var fallback = AssetDatabase.LoadAssetAtPath<GameObject>(FallbackCardPath);
            var resolverSo = new SerializedObject(resolver);
            resolverSo.FindProperty("fallbackCardPrefab").objectReferenceValue = fallback;
            resolverSo.ApplyModifiedPropertiesWithoutUndo();

            var cfg = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);

            var handManagerGo = GameObject.Find("HandManager");
            if (handManagerGo != null)
            {
                var hm = handManagerGo.GetComponent<HandManager>();
                if (hm != null)
                {
                    var so = new SerializedObject(hm);
                    so.FindProperty("handContainer").objectReferenceValue = handGo.transform;
                    so.FindProperty("cardPrefab").objectReferenceValue = fallback;
                    so.FindProperty("gameConfig").objectReferenceValue = cfg;
                    so.FindProperty("cardSpacing").floatValue = 0.28f;
                    so.FindProperty("arcRadius").floatValue = 3.4f;
                    so.FindProperty("arcAngle").floatValue = 58f;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            var tableManagerGo = GameObject.Find("TableManager");
            if (tableManagerGo != null)
            {
                var tm = tableManagerGo.GetComponent<TableManager>();
                if (tm != null)
                {
                    var so = new SerializedObject(tm);
                    so.FindProperty("tableContainer").objectReferenceValue = tableGo.transform;
                    so.FindProperty("cardPrefab").objectReferenceValue = fallback;
                    so.FindProperty("tableCenter").vector3Value = Vector3.zero;
                    so.FindProperty("cardStackSpacing").floatValue = 0.02f;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            var cam = Camera.main ?? GameObject.Find("Main Camera")?.GetComponent<Camera>();
            if (cam != null)
            {
                Undo.RecordObject(cam.transform, "Camera alpha");
                Undo.RecordObject(cam, "Camera alpha props");
                cam.transform.position = new Vector3(0f, 5.2f, -5.4f);
                cam.transform.rotation = Quaternion.Euler(52f, 0f, 0f);
                cam.fieldOfView = 42f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.35f, 0.62f, 0.85f, 1f);
            }

            if (Object.FindFirstObjectByType<OpponentSeatManager>() == null)
            {
                var seatsGo = new GameObject("OpponentSeatManager");
                Undo.RegisterCreatedObjectUndo(seatsGo, "Opponent seats");
                Undo.AddComponent<OpponentSeatManager>(seatsGo);
            }

            var light = Object.FindFirstObjectByType<Light>();
            if (light != null)
            {
                Undo.RecordObject(light.transform, "Light alpha");
                light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                light.intensity = 1.1f;
            }

            var connectBtn = GameObject.Find("ConnectButton");
            if (connectBtn != null)
                connectBtn.SetActive(false);

            var modeUi = Object.FindFirstObjectByType<GameModeSelectorUI>(FindObjectsInactive.Include);
            if (modeUi != null)
            {
                var so = new SerializedObject(modeUi);
                var panelProp = so.FindProperty("selectorPanel");
                if (panelProp != null && panelProp.objectReferenceValue == null)
                    panelProp.objectReferenceValue = modeUi.gameObject;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var gameUi = Object.FindFirstObjectByType<GameUI>(FindObjectsInactive.Include);
            if (gameUi != null)
            {
                var so = new SerializedObject(gameUi);
                if (handManagerGo != null)
                    so.FindProperty("handManager").objectReferenceValue = handManagerGo.GetComponent<HandManager>();
                if (tableManagerGo != null)
                    so.FindProperty("tableManager").objectReferenceValue = tableManagerGo.GetComponent<TableManager>();

                var playersTmp = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var t in playersTmp)
                {
                    if (t.gameObject.name == "PlayersInfoText")
                    {
                        so.FindProperty("playersInfoText").objectReferenceValue = t;
                        break;
                    }
                }

                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
        }

        private static GameObject FindOrCreate(string name)
        {
            var existing = GameObject.Find(name);
            if (existing != null) return existing;
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            return go;
        }
    }
}
