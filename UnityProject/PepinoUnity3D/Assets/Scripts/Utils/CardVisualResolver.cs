using System.Collections.Generic;
using UnityEngine;
using PepinoGame.Models;

namespace PepinoGame.Utils
{
    /// <summary>
    /// Maps Pepino cards (Spanish suits, values 1-12) to Little Games Pack French card prefabs.
    /// </summary>
    public class CardVisualResolver : MonoBehaviour
    {
        public static CardVisualResolver Instance { get; private set; }

        private const string PackCardsRoot =
            "Assets/SubstanceAssets/LittleGamesPack/Prefabs/Individual Pieces/Cards";

        [SerializeField] private GameObject fallbackCardPrefab;

        private readonly Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();
        private bool cacheBuilt;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            BuildCache();
        }

        public void SetFallbackPrefab(GameObject prefab)
        {
            fallbackCardPrefab = prefab;
        }

        public GameObject ResolvePrefab(Card card)
        {
            if (card == null)
                return fallbackCardPrefab;

            if (!cacheBuilt)
                BuildCache();

            string key = BuildKey(card.suit, card.value);
            if (prefabCache.TryGetValue(key, out GameObject prefab) && prefab != null)
                return prefab;

            Debug.LogWarning($"[CardVisualResolver] No pack prefab for {key}, using fallback");
            return fallbackCardPrefab;
        }

        public GameObject InstantiateCard(Card card, Transform parent)
        {
            GameObject prefab = ResolvePrefab(card);
            if (prefab == null)
            {
                Debug.LogError("[CardVisualResolver] No prefab available (pack or fallback)");
                return null;
            }

            return Instantiate(prefab, parent);
        }

        private void BuildCache()
        {
            prefabCache.Clear();

            string[] suits = { "♠", "♥", "♦", "♣" };
            for (int value = 1; value <= 12; value++)
            {
                foreach (string suit in suits)
                {
                    string path = GetAssetPath(suit, value);
                    GameObject prefab = LoadPrefabAtPath(path);
                    if (prefab != null)
                        prefabCache[BuildKey(suit, value)] = prefab;
                }
            }

            cacheBuilt = true;
            Debug.Log($"[CardVisualResolver] Cached {prefabCache.Count} pack card prefabs");
        }

        private static string BuildKey(string suit, int value) => $"{suit}-{value}";

        private static string GetAssetPath(string suit, int value)
        {
            string suitFolder = suit switch
            {
                "♠" => "Spades",
                "♥" => "Hearts",
                "♦" => "Diamonds",
                "♣" => "Clubs",
                _ => "Spades"
            };

            string suitSingular = suit switch
            {
                "♠" => "Spade",
                "♥" => "Heart",
                "♦" => "Diamond",
                "♣" => "Club",
                _ => "Spade"
            };

            string rank = value switch
            {
                1 => "Ace",
                11 => "Jack",
                12 => "Queen",
                _ => value.ToString()
            };

            return $"{PackCardsRoot}/{suitFolder}/Card_{suitSingular}{rank}.prefab";
        }

        private static GameObject LoadPrefabAtPath(string path)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
#else
            // Runtime builds: load from Resources mirror if present
            string resourcesPath = path
                .Replace("Assets/SubstanceAssets/LittleGamesPack/Prefabs/Individual Pieces/Cards/", "LittleGamesCards/")
                .Replace(".prefab", "");
            return Resources.Load<GameObject>(resourcesPath);
#endif
        }
    }
}
