using System;
using UnityEngine;

namespace DeadLine
{
    public class CollisionPrevention : BaseDetector
    {           
        private RaycastHit[] hits;
        private MovementData _characterData => _character.Movement.MovementData;
        private Collider[] _ownColliders;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;
        
        private void Awake()
        {
            // Cachear todos los colliders propios para ignorarlos
            _ownColliders = GetComponentsInChildren<Collider>();
        }
        
        /// <summary>
        /// Obtiene el movimiento válido ajustado para evitar colisiones
        /// </summary>
        public Vector3 GetValidMovement(Vector3 currentPosition, Vector3 intendedMovement, float height)
        {
            if (intendedMovement.sqrMagnitude < 0.0001f)
                return intendedMovement;
            
            float distance = intendedMovement.magnitude;
            Vector3 direction = intendedMovement.normalized;
            
            float radius = _characterData.CharacterRadius;
            float skinWidth = _characterData.SkinWidth;
            
            
            Vector3 point1 = currentPosition + Vector3.up * radius;
            Vector3 point2 = currentPosition + Vector3.up * (height - radius);
            
            hits = Physics.CapsuleCastAll(
                point1,
                point2,
                radius - skinWidth, 
                direction, 
                distance + skinWidth * 2,
                -1, // Todas las capas
                QueryTriggerInteraction.Ignore // Ignorar triggers
            );
            
            if (showDebugLogs)
                Debug.Log($"[CollisionPrevention] Hits detectados: {hits.Length}, distance: {distance}");
            
            if (hits.Length > 0)
            {
                float minSafeDistance = float.PositiveInfinity;
                RaycastHit? closestHit = null;
                
                foreach(RaycastHit hit in hits)
                {
                    // Verificar que no es uno de nuestros propios colliders
                    bool isOwnCollider = false;
                    foreach (Collider ownCollider in _ownColliders)
                    {
                        if (hit.collider == ownCollider)
                        {
                            isOwnCollider = true;
                            break;
                        }
                    }
                    
                    if (isOwnCollider)
                    {
                        if (showDebugLogs)
                            Debug.Log($"[CollisionPrevention] Ignorando collider propio: {hit.collider.name}");
                        continue;
                    }
                                        
                    // Detección de suelo/techo
                    float dotUp = Vector3.Dot(hit.normal, Vector3.up);
                    
                    // Ignoramos superficies horizontales
                    if (Mathf.Abs(dotUp) > 0.85f) 
                    {
                        // Verificamos si es techo o suelo
                        bool isFloor = dotUp > 0.85f && hit.point.y <= currentPosition.y + radius * 0.5f;
                        bool isCeiling = dotUp < -0.85f && hit.point.y >= currentPosition.y + height - radius * 0.5f;
                        
                        if (isFloor || isCeiling)
                        {
                            if (showDebugLogs)
                                Debug.Log($"[CollisionPrevention] Ignorando suelo/techo: {hit.collider.name}");
                            continue;
                        }
                    }
                    
                    // Calcular distancia segura con margen extra
                    float safeDistance = Mathf.Max(0, hit.distance - skinWidth * 1.5f);
                    
                    if (safeDistance < minSafeDistance)
                    {
                        minSafeDistance = safeDistance;
                        closestHit = hit;
                    }
                    
                    if (showDebugLogs)
                        Debug.Log($"[CollisionPrevention] Hit: {hit.collider.name}, distance: {hit.distance}, safe: {safeDistance}, normal: {hit.normal}");
                }
                
                if (float.IsInfinity(minSafeDistance))
                {
                    if (showDebugLogs)
                        Debug.Log("[CollisionPrevention] No hay colisiones válidas, movimiento completo");
                    return intendedMovement;
                }
                
                // Si la distancia segura es muy pequeña, impedimos movimiento
                if (minSafeDistance < skinWidth * 0.5f)
                {
                    if (showDebugLogs)
                        Debug.Log("[CollisionPrevention] Distancia muy pequeña, movimiento bloqueado");
                    return Vector3.zero;
                }
                
                if (showDebugLogs && closestHit.HasValue)
                    Debug.Log($"[CollisionPrevention] Movimiento limitado por {closestHit.Value.collider.name}, de {distance} a {minSafeDistance}");
                
                return direction * minSafeDistance;
            }
            
            // No hay colisión, permitir movimiento completo
            return intendedMovement;
        }
        
        /// <summary>
        /// Método para verificar si una posición es válida (sin overlaps)
        /// </summary>
        public bool IsPositionValid(Vector3 position, float height)
        {
            float radius = _characterData.CharacterRadius;
            float skinWidth = _characterData.SkinWidth;
            
            Vector3 point1 = position + Vector3.up * radius;
            Vector3 point2 = position + Vector3.up * (height - radius);
            
            Collider[] overlaps = Physics.OverlapCapsule(
                point1,
                point2,
                radius - skinWidth * 0.5f,
                -1,
                QueryTriggerInteraction.Ignore
            );
            
            foreach (Collider overlap in overlaps)
            {
                // Ignorar nuestros propios colliders
                bool isOwnCollider = false;
                foreach (Collider ownCollider in _ownColliders)
                {
                    if (overlap == ownCollider)
                    {
                        isOwnCollider = true;
                        break;
                    }
                }
                
                if (isOwnCollider)
                    continue;
                
                if (showDebugLogs)
                    Debug.LogWarning($"[CollisionPrevention] Overlap detectado con: {overlap.name}");
                
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Obtiene la posición válida más cercana si la actual es inválida
        /// </summary>
        public Vector3 GetNearestValidPosition(Vector3 currentPosition, float height, int maxIterations = 8)
        {
            if (IsPositionValid(currentPosition, height))
                return currentPosition;
            
            float radius = _characterData.CharacterRadius;
            float step = _characterData.SkinWidth * 2;
            
            // Intentar empujar hacia afuera en todas direcciones
            Vector3[] directions = new Vector3[]
            {
                Vector3.forward, Vector3.back, Vector3.left, Vector3.right,
                (Vector3.forward + Vector3.right).normalized,
                (Vector3.forward + Vector3.left).normalized,
                (Vector3.back + Vector3.right).normalized,
                (Vector3.back + Vector3.left).normalized
            };
            
            for (int i = 1; i <= maxIterations; i++)
            {
                float distance = step * i;
                
                foreach (Vector3 dir in directions)
                {
                    Vector3 testPosition = currentPosition + dir * distance;
                    
                    if (IsPositionValid(testPosition, height))
                    {
                        if (showDebugLogs)
                            Debug.Log($"[CollisionPrevention] Posición válida encontrada a {distance}m en dirección {dir}");
                        return testPosition;
                    }
                }
            }
            
            Debug.LogWarning("[CollisionPrevention] No se pudo encontrar posición válida!");
            return currentPosition;
        }
        
        protected override void OnDrawGizmos()
        {
            if(_character == null) return;
            
            float radius = _characterData.CharacterRadius;
            float height = _character.Movement.CurrentHeight;
            
            Vector3 center = transform.position + Vector3.up * (height * 0.5f);
            GizmosUtils.DrawWireCapsule(center, transform.rotation, radius, height, Color.green);
            
            GizmosUtils.DrawWireCapsule(center, transform.rotation, radius - _characterData.SkinWidth, height, Color.yellow);

            if (Application.isPlaying && hits != null && hits.Length > 0)
            {
                foreach (RaycastHit hit in hits)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(hit.point, 0.1f);
                    Gizmos.DrawLine(hit.point, hit.point + hit.normal * 0.5f);
                }
            }
        }
    }
}