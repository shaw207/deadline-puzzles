using UnityEngine;

namespace DeadLine
{
    public class LedgeDetector : BaseDetector
    {
        [SerializeField] private float trailUpdateTime = 0.1f; // Cada cuánto tiempo actualizar el rastro
        
        
        private Vector3 _previousPosition;
        private Vector3 _actualPosition;
        private bool _ledgeDetected;
        private Vector3 _ledgePosition;
        
        private Vector3 _previousGizmosPosition;
        private Vector3 _actualGizmosPosition;
        private RaycastHit _lastLedgeDetected;

        private float _lastTrailTime = 0f;

        public Vector3 LedgePosition => _lastLedgeDetected.point;

        private void Awake()
        {
            // Inicializar ambas posiciones con la posición actual
            _previousPosition = transform.position;
            _actualPosition = transform.position;
            _previousGizmosPosition = transform.position;
            _actualGizmosPosition = transform.position;
        }

        private void Update()
        {
            _ledgeDetected = false;
            _previousPosition = _actualPosition;
            _actualPosition = transform.position;

            
            Vector3 direction = _actualPosition - _previousPosition;
            
            RaycastHit raycastHit = new RaycastHit();
            bool hitDetected = false;
            
            if(direction.magnitude > 0.001f)
            {
               hitDetected = Physics.Raycast(_actualPosition, direction, out raycastHit, direction.magnitude);
            }

            if(hitDetected)
            {
                _ledgePosition = raycastHit.point;
                _lastLedgeDetected = raycastHit;
                
                // Solo considera como ledge escalable si la normal apunta hacia arriba
                if(Vector3.Angle(_lastLedgeDetected.normal, Vector3.up) <= 1f)
                {
                    _ledgeDetected = true;
                }
            }
            else
            {
                _ledgePosition = Vector3.positiveInfinity;
            }

            Debug();
        }

        private void Debug(){
        
            if(Time.time - _lastTrailTime >= trailUpdateTime){
                _lastTrailTime = Time.time;
                _previousGizmosPosition = _actualGizmosPosition;
                _actualGizmosPosition = _actualPosition; 
            }
        }

        public bool CheckCollision()
        {
           return _ledgeDetected;
        }

        protected override void OnDrawGizmos()
        {
            
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_lastLedgeDetected.point, 0.02f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(_lastLedgeDetected.point, _lastLedgeDetected.point + _lastLedgeDetected.normal);


            Vector3 direction = _actualGizmosPosition - _previousGizmosPosition;
            
            if(direction.magnitude > 0.01f)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(_previousGizmosPosition, _actualGizmosPosition);
                
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(_previousGizmosPosition, 0.05f);
                
                Gizmos.color = Color.yellow;
                Vector3 arrowEnd = _actualGizmosPosition + direction.normalized * 0.3f;
                Gizmos.DrawLine(_actualGizmosPosition, arrowEnd);
                Gizmos.DrawSphere(arrowEnd, 0.01f);
            }
            
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_actualGizmosPosition, 0.08f);
        }

    }
}
