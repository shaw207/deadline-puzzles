using UnityEngine;

namespace DeadLine
{
    public class ProximityPrompt : MonoBehaviour
    {
        [Header("Follow")]
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1f, 0f);
        [SerializeField] private bool faceCamera = true;

        private Transform _target;
        private Camera _cam;

        public void Bind(Transform target, Camera cam = null)
        {
            _target = target;
            _cam = cam != null ? cam : Camera.main;
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            transform.position = _target.position + worldOffset;

            if (faceCamera && _cam != null)
            {
                Vector3 dir = transform.position - _cam.transform.position;
                if (dir.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.LookRotation(dir);
            }
        }
    }
}