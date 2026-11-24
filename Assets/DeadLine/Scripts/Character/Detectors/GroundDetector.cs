using UnityEngine;

namespace DeadLine
{
    public class GroundDetector : BaseDetector
    {
        private Vector3 hitPoint;
        private RaycastHit _lastHit;
        
        public RaycastHit LastHit => _lastHit;

        public bool CheckCollision()
        {
            Vector3 origin = transform.position;
            float radius = _character.Movement.MovementData.SphereCastRadius;
            float maxDistance = _character.Movement.MovementData.SphereDistance;
            int layerMask = _character.Movement.MovementData.GroundLayer.value;
            
            bool hasHit = Physics.SphereCast(origin, radius, Vector3.down, out _lastHit, maxDistance, layerMask);
            
            if (hasHit)
                hitPoint = _lastHit.point;
            
            return hasHit;
        }

        protected override void OnDrawGizmos()
        {
            if(_character == null) return;
            
            float radius = _character.Movement.MovementData.SphereCastRadius;
            float maxDistance = _character.Movement.MovementData.SphereDistance;
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, radius);
            Gizmos.DrawWireSphere(transform.position + Vector3.down * maxDistance, radius);
            
            if (Application.isPlaying)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(hitPoint, 0.1f);
            }
        }
    }
}
