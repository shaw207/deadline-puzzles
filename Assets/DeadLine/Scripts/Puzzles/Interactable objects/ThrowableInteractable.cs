using UnityEngine;

namespace DeadLine
{
    [RequireComponent(typeof(Rigidbody))]
    public class ThrowableInteractable : MonoBehaviour, IInteractable
    {
        [Header("Throw Settings")]
        [SerializeField] private float throwTime = 0.8f; // Tiempo de vuelo en segundos

        public InteractableType Type => InteractableType.Throwable | InteractableType.Pickup;
        public Weight Weight => Weight.Normal;
        public bool IsHeld => _isHeld;

        private Rigidbody _rb;
        private Collider _collider;
        private Transform _originalParent;
        private Transform _holdPosition;
        private bool _isHeld = false;
        private bool _isThrowing = false;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();
            _originalParent = transform.parent;
        }

        public bool CanInteract(Character character)
        {
            return !_isHeld;
        }

        public void OnInteractStart(Character character)
        {
            Debug.Log("[ThrowableInteractable] OnInteractStart called.");
            // Recoger el objeto
            _isHeld = true;
            _rb.isKinematic = true;
            _holdPosition = character.Interaction.GetHoldPosition();
            
            // Desactivar colisiones mientras está sujeto
            if (_collider != null)
                _collider.enabled = false;
            
            // Posicionar el objeto en las manos del personaje
            if (_holdPosition != null)
            {
                transform.SetParent(_holdPosition);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }
            else
            {
                // Si no hay holdPosition, usar la posición del personaje
                transform.SetParent(character.transform);
                transform.localPosition = new Vector3(0, character.Movement.CurrentHeight * 0.5f, 0.5f);
                transform.localRotation = Quaternion.identity;
            }
        }

        public void OnInteracting(Character character, Vector3 inputDirection)
        {
            // Aquí puedes añadir lógica durante el holding (ej: rotación, ajuste de posición)
        }

        public void OnInteractEnd(Character character)
        {
            // Solo soltar el objeto sin lanzar (cuando pierde detección o se cancela)
            if (_isHeld)
            {
                DropObject();
                Debug.Log("[ThrowableInteractable] Objeto soltado (sin lanzar).");
            }
        }

        /// <summary>
        /// Lanza el objeto hacia donde apunta el mouse con física balística real
        /// </summary>
        public void Throw(Character character)
        {
            if (!_isHeld || _isThrowing) return;

            // Soltar el objeto
            _isHeld = false;
            transform.SetParent(_originalParent);
            
            // Reactivar físicas y colisiones
            _rb.isKinematic = false;
            if (_collider != null)
                _collider.enabled = true;
            
            // Obtener posición objetivo desde el mouse
            Vector3 targetPosition = GetTargetPositionFromMouse(character);
            
            // Calcular velocidad inicial necesaria para llegar al objetivo
            Vector3 initialVelocity = CalculateBallisticVelocity(transform.position, targetPosition, throwTime);
            
            // Aplicar velocidad
            _rb.linearVelocity = initialVelocity;
            
            Debug.Log($"[ThrowableInteractable] Lanzado con física hacia {targetPosition} con velocidad inicial {initialVelocity}");
        }

        private void DropObject()
        {
            _isHeld = false;
            transform.SetParent(_originalParent);
            _rb.isKinematic = false;
            
            // Reactivar colisiones
            if (_collider != null)
                _collider.enabled = true;
        }

        private Vector3 GetTargetPositionFromMouse(Character character)
        {
            // Obtener posición del mouse en pantalla
            Vector2 mousePos = InputManager.Instance.GetMousePosition();
            
            // Crear un rayo desde la cámara hacia el mouse
            Vector3 mouseScreenPos = new Vector3(mousePos.x, mousePos.y, character.transform.position.z - Camera.main.transform.position.z);
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mouseScreenPos);
            
            // Hacer raycast hacia el mundo
            Vector3 targetPoint;
            if (Physics.Raycast(character.transform.position, (worldPosition - character.transform.position).normalized, out RaycastHit hit, 100f))
            {
                // Si golpea algo, usar ese punto
                targetPoint = hit.point;
            }
            else
            {
                // Si no golpea nada, proyectar a cierta distancia
                targetPoint = worldPosition;
            }
            
            return targetPoint;
        }

        /// <summary>
        /// Calcula la velocidad inicial necesaria para que el objeto llegue al objetivo en el tiempo especificado
        /// usando física balística real
        /// </summary>
        private Vector3 CalculateBallisticVelocity(Vector3 startPos, Vector3 targetPos, float time)
        {
            // Desplazamiento total
            Vector3 displacement = targetPos - startPos;
            
            // Velocidad horizontal necesaria (XZ)
            Vector3 horizontalDisplacement = new Vector3(displacement.x, 0, displacement.z);
            Vector3 horizontalVelocity = horizontalDisplacement / time;
            
            // Velocidad vertical necesaria
            // Fórmula: vy = (dy - 0.5 * g * t²) / t
            // donde dy = desplazamiento vertical, g = gravedad, t = tiempo
            float gravity = Mathf.Abs(Physics.gravity.y);
            float verticalVelocity = (displacement.y - (-0.5f * gravity * time * time)) / time;
            
            // Combinar velocidades
            Vector3 velocity = horizontalVelocity;
            velocity.y = verticalVelocity;
            
            return velocity;
        }
    }
}