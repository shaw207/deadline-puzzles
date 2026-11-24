using UnityEngine;

namespace DeadLine
{
    [RequireComponent(typeof(Rigidbody))]
    public class FloatableCrate : MonoBehaviour
    {
        [SerializeField] private float floatOffset = 0.25f;
        [SerializeField] private float followStrength = 10f;
        [SerializeField] private float waterDrag = 4f;

        private Rigidbody rb;
        private bool inWater;
        private Vector2 lockedXZ;
        private WaterLevelController water;

        private float originalDrag;
        private bool originalUseGravity;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            originalDrag = rb.linearDamping;
            originalUseGravity = rb.useGravity;
        }

        private void FixedUpdate()
        {
            if (!inWater || water == null) return;

            float targetY = water.CurrentSurfaceY + floatOffset;

            var pos = rb.position;
            pos.x = lockedXZ.x;
            pos.z = lockedXZ.y;
            pos.y = Mathf.Lerp(pos.y, targetY, Time.fixedDeltaTime * followStrength);

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.MovePosition(pos);
        }

        private void OnTriggerEnter(Collider other)
        {
            var wv = other.GetComponent<WaterVolumeFitter>();
            if (wv == null) wv = other.GetComponentInParent<WaterVolumeFitter>();
            if (wv == null) return;

            water = wv.Water;
            inWater = true;

            lockedXZ = new Vector2(transform.position.x, transform.position.z);

            rb.linearDamping = waterDrag;
            rb.useGravity = false;
        }

        private void OnTriggerExit(Collider other)
        {
            var wv = other.GetComponent<WaterVolumeFitter>();
            if (wv == null) wv = other.GetComponentInParent<WaterVolumeFitter>();
            if (wv == null) return;

            if (wv.Water != water) return;

            inWater = false;
            water = null;

            rb.linearDamping = originalDrag;
            rb.useGravity = originalUseGravity;
        }
    }
}
