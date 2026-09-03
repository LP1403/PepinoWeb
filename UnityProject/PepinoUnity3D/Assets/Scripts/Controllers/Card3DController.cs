using UnityEngine;
using PepinoGame.Models;
using PepinoGame.Managers;

namespace PepinoGame.Controllers
{
    /// <summary>
    /// Controls a single 3D card (pack mesh or fallback cube).
    /// </summary>
    public class Card3DController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MeshRenderer cardRenderer;
        [SerializeField] private Collider cardCollider;

        [Header("Visual Settings")]
        [SerializeField] private Material defaultMaterial;
        [SerializeField] private Material selectedMaterial;
        [SerializeField] private Material highlightMaterial;

        private Card cardData;
        private Vector3 originalPosition;
        private Vector3 originalScale;
        private bool isSelected;
        private bool isHovered;
        private bool preservePackMaterials;
        private bool interactable = true;
        private bool playAssist;
        private GameObject assistOutline;

        public Card CardData => cardData;
        public bool IsSelected => isSelected;

        private void Awake()
        {
            if (cardRenderer == null)
                cardRenderer = GetComponentInChildren<MeshRenderer>();
            if (cardCollider == null)
                cardCollider = GetComponentInChildren<Collider>();
        }

        private void Start()
        {
            originalScale = transform.localScale;
        }

        public void Initialize(Card card, bool preservePackMaterials = false)
        {
            cardData = card;
            this.preservePackMaterials = preservePackMaterials;
            originalPosition = transform.position;
            if (originalScale == Vector3.zero)
                originalScale = transform.localScale;
            UpdateVisuals();
        }

        public void SetInteractable(bool value)
        {
            interactable = value;
            if (cardCollider == null)
                cardCollider = GetComponentInChildren<Collider>();
            if (cardCollider != null)
                cardCollider.enabled = value;
        }

        /// <summary>Soft mint rim when this card can legally answer the table (your turn).</summary>
        public void SetPlayAssist(bool enabled)
        {
            playAssist = enabled;
            EnsureAssistOutline();
            if (assistOutline != null)
                assistOutline.SetActive(playAssist);
        }

        private void EnsureAssistOutline()
        {
            if (assistOutline != null) return;

            assistOutline = new GameObject("PlayAssistOutline");
            assistOutline.transform.SetParent(transform, false);
            assistOutline.transform.localPosition = Vector3.zero;
            assistOutline.transform.localRotation = Quaternion.identity;
            assistOutline.transform.localScale = Vector3.one;
            assistOutline.layer = gameObject.layer;

            var lr = assistOutline.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = 4;
            lr.numCornerVertices = 4;
            lr.numCapVertices = 2;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.sortingOrder = 20;

            float halfW = 0.33f;
            float halfH = 0.48f;
            if (cardRenderer == null)
                cardRenderer = GetComponentInChildren<MeshRenderer>();
            if (cardRenderer != null)
            {
                var b = cardRenderer.localBounds;
                halfW = Mathf.Max(0.12f, b.extents.x * 1.08f);
                halfH = Mathf.Max(0.18f, b.extents.y * 1.08f);
            }

            lr.SetPosition(0, new Vector3(-halfW, -halfH, -0.02f));
            lr.SetPosition(1, new Vector3(-halfW, halfH, -0.02f));
            lr.SetPosition(2, new Vector3(halfW, halfH, -0.02f));
            lr.SetPosition(3, new Vector3(halfW, -halfH, -0.02f));

            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            Color assist = new Color(0.35f, 0.95f, 0.55f, 1f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", assist);
            if (mat.HasProperty("_Color")) mat.color = assist;
            lr.sharedMaterial = mat;
            lr.startColor = assist;
            lr.endColor = assist;
            lr.startWidth = 0.022f;
            lr.endWidth = 0.022f;
            assistOutline.SetActive(false);
        }

        private void UpdateVisuals()
        {
            if (cardData == null) return;
            if (!preservePackMaterials)
                UpdateMaterial();
        }

        private void UpdateMaterial()
        {
            if (preservePackMaterials || cardRenderer == null) return;

            if (isSelected && selectedMaterial != null)
                cardRenderer.material = selectedMaterial;
            else if (isHovered && highlightMaterial != null)
                cardRenderer.material = highlightMaterial;
            else if (defaultMaterial != null)
                cardRenderer.material = defaultMaterial;
        }

        public void SetSelected(bool selected, bool animate = true)
        {
            isSelected = selected;
            UpdateMaterial();

            if (!animate) return;

            if (selected)
                AnimateSelect();
            else
                AnimateDeselect();
        }

        private void AnimateSelect()
        {
            LeanTween.scale(gameObject, originalScale * 1.12f, 0.15f).setEaseOutBack();
        }

        private void AnimateDeselect()
        {
            LeanTween.scale(gameObject, originalScale, 0.15f).setEaseInBack();
        }

        public void AnimatePlay(Vector3 targetPosition, System.Action onComplete = null)
        {
            LeanTween.move(gameObject, targetPosition, 0.5f)
                .setEaseInQuad()
                .setOnComplete(() => onComplete?.Invoke());
            LeanTween.rotateY(gameObject, 360f, 0.5f).setEaseInOutQuad();
        }

        private bool IsMyTurnNow()
        {
            if (GameManager.Instance?.CurrentGameState == null) return false;
            if (NetworkManager.Instance == null) return false;
            return GameManager.Instance.CurrentGameState.IsMyTurn(NetworkManager.Instance.MyConnectionId);
        }

        private void OnMouseEnter()
        {
            if (!interactable) return;
            if (!IsMyTurnNow()) return;

            isHovered = true;
            UpdateMaterial();
        }

        private void OnMouseExit()
        {
            if (!interactable) return;

            isHovered = false;
            UpdateMaterial();
        }

        private void OnMouseDown()
        {
            if (!interactable) return;
            if (!IsMyTurnNow()) return;

            ToggleSelection();
        }

        public void ToggleSelection()
        {
            if (GameManager.Instance == null || cardData == null) return;

            bool wasSelected = GameManager.Instance.IsCardSelected(cardData);
            GameManager.Instance.ToggleCardSelection(cardData);

            bool nowSelected = GameManager.Instance.IsCardSelected(cardData);
            if (nowSelected != wasSelected)
                SetSelected(nowSelected);
        }

        public void UpdateOriginalPosition(Vector3 newPosition)
        {
            originalPosition = newPosition;
        }

        public void DestroyWithAnimation()
        {
            LeanTween.scale(gameObject, Vector3.zero, 0.25f)
                .setEaseInBack()
                .setOnComplete(() => Destroy(gameObject));
        }
    }
}
