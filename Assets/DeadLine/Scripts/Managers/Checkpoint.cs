using UnityEngine;

namespace DeadLine
{
    public class Checkpoint : MonoBehaviour
    {
        [Header("Checkpoint Settings")]
        [SerializeField] private int order = 0;
        [SerializeField] private Vector3 respawnOffset = Vector3.up * 0.5f;
        
        [Header("Detection")]
        [SerializeField] private float activationRadius = 2f;
        
        [Header("Visual Feedback")]
        [SerializeField] private GameObject activeVisual;
        [SerializeField] private GameObject inactiveVisual;
        [SerializeField] private GameObject respawnVisual;
        [SerializeField] private Color activeGizmoColor = Color.green;
        [SerializeField] private Color inactiveGizmoColor = Color.gray;
        
        private bool _isActivated = false;
        private SphereCollider _triggerCollider;
        private Rigidbody _rigidbody;

        public int Order => order;
        public bool IsActivated => _isActivated;

        private void Awake()
        {
            _rigidbody = gameObject.AddComponent<Rigidbody>();
            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;
            
            _triggerCollider = gameObject.AddComponent<SphereCollider>();
            _triggerCollider.isTrigger = true;
            _triggerCollider.radius = activationRadius;
        }

        private void OnValidate()
        {
            if (_triggerCollider != null)
                _triggerCollider.radius = activationRadius;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isActivated) return;
            
            if (other.CompareTag("Player"))
                CheckpointsManager.Instance.ReachCheckpoint(this);
        }

        public void Activate()
        {
            if (_isActivated) return;

            _isActivated = true;
            UpdateVisuals();
        }


        public void Deactivate()
        {
            _isActivated = false;
            UpdateVisuals();
        }

        public Vector3 GetRespawnPosition()
        {
            return transform.position + respawnOffset;
        }

        private void UpdateVisuals()
        {
            if (activeVisual != null)
                activeVisual.SetActive(_isActivated);
            
            if (inactiveVisual != null)
                inactiveVisual.SetActive(!_isActivated);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = _isActivated ? activeGizmoColor : inactiveGizmoColor;
            Gizmos.DrawSphere(transform.position, activationRadius);
            
            Gizmos.color = Color.cyan;
            Vector3 respawnPos = GetRespawnPosition();
            Gizmos.DrawSphere(respawnPos, 0.1f);
            Gizmos.DrawLine(transform.position, respawnPos);
        }

        private void OnDrawGizmosSelected()
        {
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, $"Checkpoint {order}");
            #endif
        }

        public GameObject GetRespawnVisual()
        {
            return respawnVisual;
        }
    }
}
