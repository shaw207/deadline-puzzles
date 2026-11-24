using UnityEngine;

namespace DeadLine
{
    [DisallowMultipleComponent]
    public class InteractableTriggerForwarder : MonoBehaviour
    {
        [SerializeField] private InteractableObjectsBase owner;

        public void SetOwner(InteractableObjectsBase o) => owner = o;

        private void Awake()
        {
            if (owner == null)
                owner = GetComponentInParent<InteractableObjectsBase>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (owner == null) return;
            owner.NotifyTriggerEnter(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (owner == null) return;
            owner.NotifyTriggerExit(other);
        }
    }
}