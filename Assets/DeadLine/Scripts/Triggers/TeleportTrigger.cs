using System.Collections;
using UnityEngine;

namespace DeadLine
{
    [RequireComponent(typeof(BoxCollider))]
    public class TeleportTrigger : MonoBehaviour
    {
        [SerializeField] private Transform teleportPosition;
        [SerializeField] private float fadeOutDuration = 2f;
        [SerializeField] private float fadeInDuration = 2f;
        [Header("Visualization")]
        [SerializeField] private Color gizmosColor;
        
        private BoxCollider _boxCollider;

        private void Awake()
        {
            _boxCollider = GetComponent<BoxCollider>();
            _boxCollider.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if(other.GetComponent<Player>())
                StartCoroutine(Teleport(other.gameObject));
        }

        private IEnumerator Teleport(GameObject other)
        {
            OriginalDemoFlow.Instance.BlockPlayerInput(fadeOutDuration + fadeInDuration);
            OriginalDemoFlow.Instance?.FadeOut(fadeOutDuration);
            yield return new WaitForSeconds(fadeOutDuration);
            other.transform.position = teleportPosition.position;
            OriginalDemoFlow.Instance?.FadeIn(fadeInDuration);
        }

        private void OnValidate()
        {
            _boxCollider = GetComponent<BoxCollider>();
        }
    
        void OnDrawGizmos()
        {
            Gizmos.color = gizmosColor;
            Gizmos.DrawCube(transform.position + _boxCollider.center, _boxCollider.size);
            if (teleportPosition)
            {
                Gizmos.DrawLine(transform.position + _boxCollider.center, teleportPosition.position);
                Gizmos.DrawSphere(teleportPosition.position, 0.3f);
            }
        }
    }
}
