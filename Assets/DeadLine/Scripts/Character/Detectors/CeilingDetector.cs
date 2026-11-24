using UnityEngine;

namespace DeadLine
{
    public class CeilingDetector : BaseDetector
    {
        private Vector3 hitPoint;

        public bool CheckCollision()
        {
            Vector3 origin = new Vector3(_character.transform.position.x, _character.transform.position.y + _character.Movement.CurrentHeight, _character.transform.position.z);
            float radius = _character.Movement.MovementData.SphereCastRadius;
            float maxDistance = _character.Movement.MovementData.SphereDistance;

            RaycastHit hit;
            bool hasHit = Physics.SphereCast(origin, radius, Vector3.up, out hit, maxDistance);

            if (hasHit)
                hitPoint = hit.point;
            
            return hasHit;
        }

        protected override void OnDrawGizmos()
        {
            if(_character == null) return;
            
            if (Application.isPlaying)
            {
                Vector3 origin = new Vector3(_character.transform.position.x, _character.transform.position.y + _character.Movement.CurrentHeight, _character.transform.position.z);
                float radius = _character.Movement.MovementData.SphereCastRadius; 
                float maxDistance = _character.Movement.MovementData.SphereDistance;
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(origin, radius);
                Gizmos.DrawWireSphere(origin + Vector3.up * maxDistance, radius);
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(hitPoint, 0.1f);
            }
        }
    }
}
