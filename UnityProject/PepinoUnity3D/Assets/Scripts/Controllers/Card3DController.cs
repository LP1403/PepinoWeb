using UnityEngine;
using PepinoGame.Models;
using PepinoGame.Managers;

namespace PepinoGame.Controllers
{
    /// <summary>
    /// Controla una carta individual en 3D
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
        private bool isSelected = false;
        private bool isHovered = false;

        public Card CardData => cardData;
        public bool IsSelected => isSelected;

        private void Start()
        {
            originalScale = transform.localScale;
        }

        /// <summary>
        /// Inicializa la carta con sus datos
        /// </summary>
        public void Initialize(Card card)
        {
            cardData = card;
            originalPosition = transform.position;
            UpdateVisuals();
        }

        /// <summary>
        /// Actualiza los visuales de la carta
        /// </summary>
        private void UpdateVisuals()
        {
            if (cardData == null) return;

            // Aquí cargarías el sprite/textura de la carta
            // Por ahora solo cambiamos el material según el estado
            UpdateMaterial();

            // TODO: Cargar sprite de la carta según suit y value
            // Ejemplo: Resources.Load<Sprite>($"Cards/{cardData.suit}_{cardData.value}");
        }

        /// <summary>
        /// Actualiza el material según el estado
        /// </summary>
        private void UpdateMaterial()
        {
            if (cardRenderer == null) return;

            if (isSelected && selectedMaterial != null)
            {
                cardRenderer.material = selectedMaterial;
            }
            else if (isHovered && highlightMaterial != null)
            {
                cardRenderer.material = highlightMaterial;
            }
            else if (defaultMaterial != null)
            {
                cardRenderer.material = defaultMaterial;
            }
        }

        /// <summary>
        /// Selecciona o deselecciona la carta
        /// </summary>
        public void SetSelected(bool selected, bool animate = true)
        {
            isSelected = selected;
            UpdateMaterial();

            if (animate)
            {
                if (selected)
                {
                    AnimateSelect();
                }
                else
                {
                    AnimateDeselect();
                }
            }
        }

        /// <summary>
        /// Anima la selección de la carta
        /// </summary>
        private void AnimateSelect()
        {
            // Elevar la carta
            Vector3 targetPos = originalPosition + Vector3.up * 0.5f;
            LeanTween.move(gameObject, targetPos, 0.2f).setEaseOutBack();
            
            // Escalar ligeramente
            LeanTween.scale(gameObject, originalScale * 1.1f, 0.2f).setEaseOutBack();
        }

        /// <summary>
        /// Anima la deselección de la carta
        /// </summary>
        private void AnimateDeselect()
        {
            LeanTween.move(gameObject, originalPosition, 0.2f).setEaseInBack();
            LeanTween.scale(gameObject, originalScale, 0.2f).setEaseInBack();
        }

        /// <summary>
        /// Anima la carta siendo jugada
        /// </summary>
        public void AnimatePlay(Vector3 targetPosition, System.Action onComplete = null)
        {
            LeanTween.move(gameObject, targetPosition, 0.5f)
                .setEaseInQuad()
                .setOnComplete(() =>
                {
                    onComplete?.Invoke();
                });

            // Rotar mientras vuela
            LeanTween.rotateY(gameObject, 360f, 0.5f).setEaseInOutQuad();
        }

        #region Mouse Interactions

        private void OnMouseEnter()
        {
            // Solo permitir hover si es mi turno y la carta está en mi mano
            if (GameManager.Instance?.CurrentGameState?.IsMyTurn(NetworkManager.Instance.MyConnectionId) ?? false)
            {
                isHovered = true;
                UpdateMaterial();
                
                // Pequeña elevación en hover
                if (!isSelected)
                {
                    LeanTween.moveY(gameObject, originalPosition.y + 0.2f, 0.15f).setEaseOutQuad();
                }
            }
        }

        private void OnMouseExit()
        {
            isHovered = false;
            UpdateMaterial();

            // Volver a posición original si no está seleccionada
            if (!isSelected)
            {
                LeanTween.moveY(gameObject, originalPosition.y, 0.15f).setEaseInQuad();
            }
        }

        private void OnMouseDown()
        {
            // Solo permitir click si es mi turno
            if (GameManager.Instance?.CurrentGameState?.IsMyTurn(NetworkManager.Instance.MyConnectionId) ?? false)
            {
                ToggleSelection();
            }
        }

        #endregion

        /// <summary>
        /// Alterna la selección de la carta
        /// </summary>
        public void ToggleSelection()
        {
            if (GameManager.Instance == null) return;

            GameManager.Instance.ToggleCardSelection(cardData);
            SetSelected(!isSelected);
        }

        /// <summary>
        /// Actualiza la posición original (usado por HandManager)
        /// </summary>
        public void UpdateOriginalPosition(Vector3 newPosition)
        {
            originalPosition = newPosition;
            
            if (!isSelected && !isHovered)
            {
                transform.position = originalPosition;
            }
        }

        /// <summary>
        /// Destruye la carta con animación
        /// </summary>
        public void DestroyWithAnimation()
        {
            LeanTween.scale(gameObject, Vector3.zero, 0.3f)
                .setEaseInBack()
                .setOnComplete(() => Destroy(gameObject));
        }
    }
}

