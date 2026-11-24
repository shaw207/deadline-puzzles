using UnityEngine;

namespace DeadLine
{
    public class PushableObject : MonoBehaviour, IInteractable
    {
        [Header("Push Settings")]
        [SerializeField] private Weight weight;

        private Rigidbody _rigidbody;           
        private Vector3 _currentVelocityRef;

        public InteractableType Type => InteractableType.Push; 
        public Weight Weight => weight;

        void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            if(!_rigidbody)
                _rigidbody = this.gameObject.AddComponent<Rigidbody>();
        }   

        public bool CanInteract(Character character)
        {
            return true;
        }

        public void OnInteractStart(Character character)
        {
        }

        public void OnInteracting(Character character, Vector3 inputDirection)
        {
            Vector3 direction = character.transform.forward * inputDirection.magnitude;
            Vector3 targetVelocity = direction / weight.GetValue();

            _rigidbody.linearVelocity = Vector3.SmoothDamp(_rigidbody.linearVelocity, targetVelocity, ref _currentVelocityRef, 0.1f);

        }

        public void OnInteractEnd(Character character)
        {
        }

    }
}