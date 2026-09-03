using UnityEngine;
using PepinoGame.Models;

namespace PepinoGame.Utils
{
    /// <summary>
    /// Applies Pepino branding: custom card backs + suit tint overlay for face-up cards.
    /// </summary>
    public static class PepinoCardSkin
    {
        private static Texture2D cardBack;
        private static Material backMaterial;
        private static bool loaded;

        private static readonly Color Spades = new Color(0.2f, 0.35f, 0.75f);   // Policías
        private static readonly Color Hearts = new Color(0.85f, 0.2f, 0.25f);   // Médicos
        private static readonly Color Diamonds = new Color(0.75f, 0.55f, 0.15f); // Soldados
        private static readonly Color Clubs = new Color(0.45f, 0.25f, 0.65f);   // Bufones

        public static void EnsureLoaded()
        {
            if (loaded) return;
            loaded = true;
            cardBack = Resources.Load<Texture2D>("PepinoArt/pepino_card_back");
            if (cardBack != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                backMaterial = new Material(shader);
                backMaterial.mainTexture = cardBack;
                if (backMaterial.HasProperty("_BaseMap"))
                    backMaterial.SetTexture("_BaseMap", cardBack);
                if (backMaterial.HasProperty("_Color"))
                    backMaterial.color = Color.white;
                if (backMaterial.HasProperty("_BaseColor"))
                    backMaterial.SetColor("_BaseColor", Color.white);
            }
        }

        public static Color SuitColor(string suit)
        {
            return suit switch
            {
                "♠" => Spades,
                "♥" => Hearts,
                "♦" => Diamonds,
                "♣" => Clubs,
                _ => Color.white
            };
        }

        public static string SuitLabel(string suit)
        {
            return suit switch
            {
                "♠" => "Policías",
                "♥" => "Médicos",
                "♦" => "Soldados",
                "♣" => "Bufones",
                _ => suit
            };
        }

        public static void ApplyFaceUp(GameObject cardObj, Card card)
        {
            if (cardObj == null || card == null) return;

            try
            {
                EnsureLoaded();

                Color tint = SuitColor(card.suit);
                Color soft = Color.Lerp(Color.white, tint, 0.28f);

                foreach (var r in cardObj.GetComponentsInChildren<Renderer>())
                {
                    if (r == null) continue;
                    var mats = r.materials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        if (mats[i] == null) continue;
                        var m = new Material(mats[i]);
                        if (m.HasProperty("_Color"))
                            m.color = soft;
                        if (m.HasProperty("_BaseColor"))
                            m.SetColor("_BaseColor", soft);
                        mats[i] = m;
                    }

                    r.materials = mats;
                }

                EnsureSuitBadge(cardObj, card);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[PepinoCardSkin] ApplyFaceUp: {ex.Message}");
            }
        }

        public static void ApplyBack(GameObject cardObj)
        {
            try
            {
                EnsureLoaded();
                if (backMaterial == null || cardObj == null) return;

                foreach (var r in cardObj.GetComponentsInChildren<Renderer>())
                {
                    if (r == null) continue;
                    int count = r.sharedMaterials != null ? r.sharedMaterials.Length : 1;
                    var mats = new Material[Mathf.Max(1, count)];
                    for (int i = 0; i < mats.Length; i++)
                        mats[i] = backMaterial;
                    r.sharedMaterials = mats;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[PepinoCardSkin] ApplyBack: {ex.Message}");
            }
        }

        /// <summary>Shared Pepino back material (dark green + logo) for rival fans / deck.</summary>
        public static Material GetBackMaterial()
        {
            EnsureLoaded();
            return backMaterial;
        }

        private static void EnsureSuitBadge(GameObject cardObj, Card card)
        {
            if (cardObj.transform.Find("PepinoSuitBadge") != null) return;

            string texName = card.suit switch
            {
                "♠" => "PepinoArt/pepino_suit_policias",
                "♥" => "PepinoArt/pepino_suit_medicos",
                "♦" => "PepinoArt/pepino_suit_soldados",
                "♣" => "PepinoArt/pepino_suit_bufones",
                _ => null
            };
            if (texName == null) return;

            var tex = Resources.Load<Texture2D>(texName);
            if (tex == null) return;

            var badge = GameObject.CreatePrimitive(PrimitiveType.Quad);
            badge.name = "PepinoSuitBadge";
            badge.transform.SetParent(cardObj.transform, false);
            // Slightly in front of card face (pack face ≈ +Z)
            badge.transform.localPosition = new Vector3(0f, 0f, 0.02f);
            badge.transform.localRotation = Quaternion.identity;
            badge.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);

            Object.Destroy(badge.GetComponent<Collider>());

            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Texture")
                         ?? Shader.Find("Standard");
            var mat = new Material(shader);
            mat.mainTexture = tex;
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", tex);
            badge.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }
}
