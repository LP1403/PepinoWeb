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
            // Lift toward the player camera, not world +Y (cards are tilted)
            Vector3 lift = Camera.main != null
                ? (Camera.main.transform.position - originalPosition).normalized * 0.22f
                : Vector3.up * 0.2f;
            Vector3 targetPos = originalPosition + lift;
            LeanTween.move(gameObject, targetPos, 0.2f).setEaseOutBack();
            LeanTween.scale(gameObject, originalScale * 1.1f, 0.2f).setEaseOutBack();
        }

        private void AnimateDeselect()
        {
            LeanTween.move(gameObject, originalPosition, 0.2f).setEaseInBack();
            LeanTween.scale(gameObject, originalScale, 0.2f).setEaseInBack();
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

            isHovered = true;
            UpdateMaterial();
            if (!isSelected)
                LeanTween.moveY(gameObject, originalPosition.y + 0.08f, 0.15f).setEaseOutQuad();
        }

        private void OnMouseExit()
        {
            if (!interactable) return;

            isHovered = false;
            UpdateMaterial();
            if (!isSelected)
                LeanTween.moveY(gameObject, originalPosition.y, 0.15f).setEaseInQuad();
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
            if (!isSelected && !isHovered)
                transform.position = originalPosition;
        }

        public void DestroyWithAnimation()
        {
            LeanTween.scale(gameObject, Vector3.zero, 0.25f)
                .setEaseInBack()
                .setOnComplete(() => Destroy(gameObject));
        }
    }
}
