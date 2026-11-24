using UnityEngine;

namespace DeadLine
{
    public class PullableObject : MonoBehaviour, IInteractable
    {
        [Header("Pull Settings")]
        [SerializeField] private Weight weight;

        private Rigidbody _rigidbody;           
        private float _distanceToCharacter = 0f;

        public InteractableType Type => InteractableType.Pull; 
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
            _distanceToCharacter = Vector3.Distance(character.transform.position, transform.position);
        }

        public void OnInteracting(Character character, Vector3 inputDirection)
        {   
            Vector3 targetPosition = character.transform.position + character.transform.forward * _distanceToCharacter;
            targetPosition.y = transform.position.y;
            
            Vector3 newPosition = Vector3.Lerp(transform.position, targetPosition, Time.fixedDeltaTime * 10f);
            
            transform.position = newPosition;
            
            _rigidbody.MovePosition(newPosition);

   
        }

        public void OnInteractEnd(Character character)
        {
        }
    }
}