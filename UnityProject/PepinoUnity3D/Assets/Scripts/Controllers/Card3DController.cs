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
            // HandManager.LateUpdate applies the selected lift in viewport space
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

        private void OnMouseEnter()
        {
            if (!interactable) return;
            if (!(GameManager.Instance?.CurrentGameState?.IsMyTurn(NetworkManager.Instance.MyConnectionId) ?? false))
                return;

            // Viewport layout owns positions each frame — skip LeanTween hover move
            isHovered = true;
            UpdateMaterial();
            return;
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
            if (!(GameManager.Instance?.CurrentGameState?.IsMyTurn(NetworkManager.Instance.MyConnectionId) ?? false))
                return;

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
