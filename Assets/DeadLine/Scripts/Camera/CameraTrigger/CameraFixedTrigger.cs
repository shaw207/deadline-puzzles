using UnityEngine;

namespace DeadLine
{
    public class CameraFixedTrigger : MonoBehaviour
    {
        [Header("Fixed Camera Setup")]
        [SerializeField] private Transform cameraPoint;   // camera position
        [SerializeField] private Transform lookPoint;     // camera look-at target 

        [Header("Look Behavior")]
        [SerializeField] private bool followPlayer = true;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            Camera cam = Camera.main;
            if (cam == null || cameraPoint == null) return;

            CameraController camLogic = cam.GetComponent<CameraController>();
            if (camLogic == null) return;

            camLogic.SetFixedMode(cameraPoint, lookPoint, followPlayer);
        }
    }
}