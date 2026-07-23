using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PepinoGame.Controllers
{
    /// <summary>
    /// First-person hands at bottom of screen (mockup).
    /// Auto-wires FPHands pack prefabs when present; otherwise procedural cubes.
    /// </summary>
    public class PlayerHandsView : MonoBehaviour
    {
        private const string LeftPrefabPath = "Assets/FirstPersonHands/Prefabs/MaleHand_L.prefab";
        private const string RightPrefabPath = "Assets/FirstPersonHands/Prefabs/MaleHand_R.prefab";
        private const string BothPrefabPath = "Assets/FirstPersonHands/Prefabs/firstPersonHand.prefab";

        [Header("Optional imported FPS hands")]
        [SerializeField] private GameObject leftHandPrefab;
        [SerializeField] private GameObject rightHandPrefab;
        [SerializeField] private GameObject bothHandsPrefab;

        [Header("Placement (camera-local)")]
        [SerializeField] private Vector3 leftLocalPos = new Vector3(-0.28f, -0.42f, 0.52f);
        [SerializeField] private Vector3 rightLocalPos = new Vector3(0.28f, -0.42f, 0.52f);
        [SerializeField] private Vector3 bothLocalPos = new Vector3(0f, -0.4f, 0.5f);
        [SerializeField] private Vector3 leftEuler = new Vector3(12f, 200f, 18f);
        [SerializeField] private Vector3 rightEuler = new Vector3(12f, 160f, -18f);
        [SerializeField] private float handScale = 0.42f;

        [Header("Procedural fallback")]
        [SerializeField] private Color skinColor = new Color(0.82f, 0.62f, 0.48f, 1f);

        private Transform handsRoot;
        private bool built;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Object.FindAnyObjectByType<PlayerHandsView>() != null)
                return;

            var go = new GameObject("PlayerHandsView");
            go.AddComponent<PlayerHandsView>();
        }

        private void Awake()
        {
            TryAutoAssignPrefabs();
        }

        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;

            if (!built)
                Build(cam.transform);

            if (handsRoot == null) return;

            handsRoot.SetParent(cam.transform, false);
            handsRoot.localPosition = Vector3.zero;
            handsRoot.localRotation = Quaternion.identity;
        }

        private void TryAutoAssignPrefabs()
        {
#if UNITY_EDITOR
            if (leftHandPrefab == null)
                leftHandPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LeftPrefabPath);
            if (rightHandPrefab == null)
                rightHandPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RightPrefabPath);
            if (bothHandsPrefab == null && (leftHandPrefab == null || rightHandPrefab == null))
                bothHandsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BothPrefabPath);
#endif
        }

        private void Build(Transform cam)
        {
            built = true;
            handsRoot = new GameObject("FP_Hands").transform;
            handsRoot.SetParent(cam, false);

            if (leftHandPrefab != null && rightHandPrefab != null)
            {
                SpawnPrefab(leftHandPrefab, leftLocalPos, leftEuler);
                SpawnPrefab(rightHandPrefab, rightLocalPos, rightEuler);
                Debug.Log("[PlayerHandsView] Using MaleHand_L / MaleHand_R from FPHands pack");
                return;
            }

            if (bothHandsPrefab != null)
            {
                SpawnPrefab(bothHandsPrefab, bothLocalPos, new Vector3(0f, 180f, 0f));
                Debug.Log("[PlayerHandsView] Using firstPersonHand prefab from FPHands pack");
                return;
            }

            Debug.LogWarning("[PlayerHandsView] FPHands prefabs not found — using procedural hands");
            SpawnProceduralHand("LeftHand", leftLocalPos, mirror: true);
            SpawnProceduralHand("RightHand", rightLocalPos, mirror: false);
        }

        private void SpawnPrefab(GameObject prefab, Vector3 localPos, Vector3 euler)
        {
            var go = Instantiate(prefab, handsRoot);
            go.name = prefab.name + "_Runtime";
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = Vector3.one * handScale;
            StripPhysics(go);

            // Don't let Animator fight placement unless clips are set up for cards
            foreach (var anim in go.GetComponentsInChildren<Animator>())
                anim.enabled = false;
        }

        private void SpawnProceduralHand(string name, Vector3 localPos, bool mirror)
        {
            var hand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hand.name = name;
            hand.transform.SetParent(handsRoot, false);
            hand.transform.localPosition = localPos;
            hand.transform.localRotation = Quaternion.Euler(75f, mirror ? 18f : -18f, mirror ? 8f : -8f);
            hand.transform.localScale = new Vector3(0.22f, 0.08f, 0.32f);

            var palmMat = new Material(Shader.Find("Universal Render Pipeline/Lit")
                                       ?? Shader.Find("Standard"));
            palmMat.color = skinColor;
            hand.GetComponent<Renderer>().sharedMaterial = palmMat;
            StripPhysics(hand);

            for (int i = 0; i < 4; i++)
            {
                var finger = GameObject.CreatePrimitive(PrimitiveType.Cube);
                finger.name = $"Finger_{i}";
                finger.transform.SetParent(hand.transform, false);
                float x = (i - 1.5f) * 0.22f;
                finger.transform.localPosition = new Vector3(x, 0.15f, 0.55f);
                finger.transform.localScale = new Vector3(0.18f, 0.35f, 0.55f);
                finger.GetComponent<Renderer>().sharedMaterial = palmMat;
                StripPhysics(finger);
            }

            var thumb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            thumb.name = "Thumb";
            thumb.transform.SetParent(hand.transform, false);
            thumb.transform.localPosition = new Vector3(mirror ? 0.55f : -0.55f, 0f, 0.15f);
            thumb.transform.localRotation = Quaternion.Euler(0f, 0f, mirror ? -35f : 35f);
            thumb.transform.localScale = new Vector3(0.2f, 0.3f, 0.4f);
            thumb.GetComponent<Renderer>().sharedMaterial = palmMat;
            StripPhysics(thumb);
        }

        private static void StripPhysics(GameObject go)
        {
            foreach (var col in go.GetComponentsInChildren<Collider>())
                Object.Destroy(col);
            foreach (var rb in go.GetComponentsInChildren<Rigidbody>())
                Object.Destroy(rb);
        }

        public void SetHandPrefabs(GameObject left, GameObject right)
        {
            leftHandPrefab = left;
            rightHandPrefab = right;
            if (handsRoot != null)
                Destroy(handsRoot.gameObject);
            built = false;
        }
    }
}
