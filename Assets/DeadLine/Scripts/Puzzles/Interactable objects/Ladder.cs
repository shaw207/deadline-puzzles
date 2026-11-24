using UnityEngine;

namespace DeadLine
{
    public class Ladder : MonoBehaviour, IInteractable
    {
        [Header("Ladder Settings")]
        [Tooltip("Punto de montaje superior de la escalera")]
        [SerializeField] private Transform topMountPoint;
        
        [Tooltip("Punto de montaje inferior de la escalera")]
        [SerializeField] private Transform bottomMountPoint;
        
        [Tooltip("Punto de desmontaje superior (donde el personaje termina al salir por arriba)")]
        [SerializeField] private Transform topDismountPoint;
        
        [Tooltip("Punto de desmontaje inferior (donde el personaje termina al salir por abajo)")]
        [SerializeField] private Transform bottomDismountPoint;
        
        [Tooltip("Altura total de la escalera")]
        [SerializeField] private float ladderHeight = 5f;
        
        [Header("Detection")]
        [Tooltip("Distancia mínima para poder interactuar con la escalera")]
        [SerializeField] private float interactionDistance = 1.5f;
        [Tooltip("Offset vertical para el área de interacción")]
        [SerializeField] private float interactionHeightOffset = 5f;

        [Header("Lateral Movement")]
        [Tooltip("Máximo offset lateral permitido desde el centro de la escalera")]
        [SerializeField] private float maxLateralOffset = 0.5f;

        public InteractableType Type => InteractableType.Ladder;
        public Weight Weight => Weight.None;

        public float LadderHeight => ladderHeight;
        public Transform TopMountPoint => topMountPoint;
        public Transform BottomMountPoint => bottomMountPoint;

        // Cache para optimización
        private Vector3 cachedBoxCenter;
        private Vector3 cachedHalfExtents;
        private float cachedHeight;
        private bool cacheValid = false;

        private void OnValidate()
        {
            if(topMountPoint == null || bottomMountPoint == null)
            {
                Debug.LogWarning("[Ladder] Los puntos de montaje no pueden ser nulos.");
            }
            if (topMountPoint != null && bottomMountPoint != null)
            {
                ladderHeight = topMountPoint.position.y - bottomMountPoint.position.y;
            }
            cacheValid = false;
        }

        private void Start()
        {
            UpdateCache();
        }

        private void UpdateCache()
        {
            if (topMountPoint == null || bottomMountPoint == null) return;
            
            cachedHeight = (topMountPoint.position - bottomMountPoint.position).magnitude;
            Vector3 center = (topMountPoint.position + bottomMountPoint.position) / 2f;
            cachedBoxCenter = center + transform.forward * (interactionDistance / 2f);
            cachedHalfExtents = new Vector3(
                interactionDistance / 2f, 
                (cachedHeight + interactionHeightOffset) / 2f, 
                interactionDistance / 2f
            );
            cacheValid = true;
        }

        public bool CanInteract(Character character)
        {
            if (character == null || topMountPoint == null || bottomMountPoint == null) return false;
            
            if (!cacheValid) UpdateCache();
            
            // Transformar a espacio local (ultra rápido)
            Vector3 localPos = Quaternion.Inverse(transform.rotation) * (character.transform.position - cachedBoxCenter);
            
            // Verificar límites
            return Mathf.Abs(localPos.x) <= cachedHalfExtents.x &&
                   Mathf.Abs(localPos.y) <= cachedHalfExtents.y &&
                   Mathf.Abs(localPos.z) <= cachedHalfExtents.z;
        }

        public void OnInteractStart(Character character)
        {
            Debug.Log($"[Ladder] Personaje comenzó interacción con escalera");
        }

        public void OnInteracting(Character character, Vector3 inputDirection)
        {
            // El MovementController maneja todo el movimiento en la escalera
        }

        public void OnInteractEnd(Character character)
        {
            Debug.Log($"[Ladder] Personaje terminó interacción con escalera");
        }

        /// <summary>
        /// Obtiene el centro de la escalera a una altura Y específica
        /// </summary>
        public Vector3 GetCenterLineAtHeight(float yPosition)
        {
            // Clampear la altura a los límites de la escalera
            float clampedY = ClampToLadderBounds(yPosition);
            
            // El centro de la escalera es una línea vertical entre los mount points
            Vector3 bottomPos = bottomMountPoint.position;
            Vector3 topPos = topMountPoint.position;
            
            // Calcular la posición en el centro de la escalera a esta altura
            float t = Mathf.InverseLerp(bottomPos.y, topPos.y, clampedY);
            return Vector3.Lerp(bottomPos, topPos, t);
        }

        /// <summary>
        /// Obtiene el offset lateral actual desde el centro de la escalera
        /// </summary>
        public float GetLateralOffset(Vector3 position)
        {
            Vector3 centerLine = GetCenterLineAtHeight(position.y);
            Vector3 toPosition = position - centerLine;
            return Vector3.Dot(toPosition, transform.right);
        }

        /// <summary>
        /// Verifica si una posición está dentro del rango lateral permitido
        /// </summary>
        public bool IsWithinLateralBounds(Vector3 position)
        {
            float offset = GetLateralOffset(position);
            return Mathf.Abs(offset) <= maxLateralOffset;
        }

        /// <summary>
        /// Limita una posición al rango lateral permitido
        /// </summary>
        public Vector3 ClampToLateralBounds(Vector3 position)
        {
            Vector3 centerLine = GetCenterLineAtHeight(position.y);
            Vector3 toPosition = position - centerLine;
            float lateralOffset = Vector3.Dot(toPosition, transform.right);
            
            // Limitar el offset
            lateralOffset = Mathf.Clamp(lateralOffset, -maxLateralOffset, maxLateralOffset);
            
            return centerLine + transform.right * lateralOffset;
        }

        /// <summary>
        /// Obtiene la posición de montaje más cercana al personaje (OPTIMIZADO)
        /// </summary>
        public Vector3 GetNearestMountPosition(Vector3 characterPosition)
        {
            if (topMountPoint == null || bottomMountPoint == null)
            {
                Debug.LogWarning("[Ladder] Puntos de montaje no configurados");
                return transform.position;
            }

            // Usar sqrMagnitude para evitar raíz cuadrada
            float distToTopSqr = (characterPosition - topMountPoint.position).sqrMagnitude;
            float distToBottomSqr = (characterPosition - bottomMountPoint.position).sqrMagnitude;

            return distToTopSqr < distToBottomSqr ? topMountPoint.position : bottomMountPoint.position;
        }

        /// <summary>
        /// Obtiene la dirección hacia la que mira la escalera (para rotar al personaje)
        /// </summary>
        public Vector3 GetFacingDirection()
        {
            return -transform.forward;
        }

        /// <summary>
        /// Obtiene la posición de desmontaje superior
        /// </summary>
        public Vector3 GetTopDismountPosition()
        {
            return topDismountPoint != null ? topDismountPoint.position : topMountPoint.position;
        }

        /// <summary>
        /// Obtiene la posición de desmontaje inferior
        /// </summary>
        public Vector3 GetBottomDismountPosition()
        {
            return bottomDismountPoint != null ? bottomDismountPoint.position : bottomMountPoint.position;
        }

        /// <summary>
        /// Obtiene la posición de desmontaje más cercana (OPTIMIZADO)
        /// </summary>
        public Vector3 GetNearestDismountPosition(Vector3 characterPosition)
        {
            Vector3 topPos = GetTopDismountPosition();
            Vector3 bottomPos = GetBottomDismountPosition();
            
            // Usar sqrMagnitude para evitar raíz cuadrada
            float distToTopSqr = (characterPosition - topPos).sqrMagnitude;
            float distToBottomSqr = (characterPosition - bottomPos).sqrMagnitude;

            return distToTopSqr < distToBottomSqr ? topPos : bottomPos;
        }

        /// <summary>
        /// Verifica si una posición Y está dentro de los límites de la escalera
        /// </summary>
        public bool IsWithinLadderBounds(float yPosition)
        {
            float bottomY = bottomMountPoint.position.y;
            float topY = topMountPoint.position.y;
            return yPosition >= bottomY && yPosition <= topY;
        }

        /// <summary>
        /// Limita una posición Y a los límites de la escalera
        /// </summary>
        public float ClampToLadderBounds(float yPosition)
        {
            float bottomY = bottomMountPoint.position.y;
            float topY = topMountPoint.position.y;
            return Mathf.Clamp(yPosition, bottomY, topY);
        }

        private void OnDrawGizmosSelected()
        {
            if (topMountPoint == null || bottomMountPoint == null) return;
            
            if (!cacheValid) UpdateCache();
            
            // Dibujar área de interacción
            Vector3 boxSize = new Vector3(
                cachedHalfExtents.x * 2f, 
                cachedHalfExtents.y * 2f, 
                cachedHalfExtents.z * 2f
            );
            
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.matrix = Matrix4x4.TRS(cachedBoxCenter, transform.rotation, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, boxSize);
            Gizmos.DrawWireCube(Vector3.zero, boxSize);
            Gizmos.matrix = Matrix4x4.identity;
            
            // Dibujar línea central de la escalera
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(bottomMountPoint.position, topMountPoint.position);
            
            // Dibujar límites laterales
            Gizmos.color = Color.red;
            Vector3 lateralRight = transform.right * maxLateralOffset;
            
            // Línea lateral derecha
            Gizmos.DrawLine(
                bottomMountPoint.position + lateralRight, 
                topMountPoint.position + lateralRight
            );
            
            // Línea lateral izquierda
            Gizmos.DrawLine(
                bottomMountPoint.position - lateralRight, 
                topMountPoint.position - lateralRight
            );
            
            // Dibujar líneas horizontales cada metro para visualizar mejor
            int steps = Mathf.CeilToInt(cachedHeight);
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector3 pos = Vector3.Lerp(bottomMountPoint.position, topMountPoint.position, t);
                Gizmos.DrawLine(pos - lateralRight, pos + lateralRight);
            }
        }
    }
}