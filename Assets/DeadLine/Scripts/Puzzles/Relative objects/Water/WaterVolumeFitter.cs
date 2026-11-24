using UnityEngine;

namespace DeadLine
{
    [RequireComponent(typeof(BoxCollider))]
    public class WaterVolumeFitter : MonoBehaviour
    {
        [SerializeField] private WaterLevelController water;
        [SerializeField] private float padding = 0.1f;

        private BoxCollider box;

        public WaterLevelController Water => water;

        private void Awake()
        {
            box = GetComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = Vector3.zero;
        }

        private void Update()
        {
            float bottomY = water.BottomY;
            float topY = water.CurrentSurfaceY;

            float height = Mathf.Max(0f, topY - bottomY);
            float midY = (topY + bottomY) * 0.5f;

            var p = transform.position;
            p.y = midY;
            transform.position = p;

            var s = box.size;
            s.y = height + padding;
            box.size = s;
        }
    }
}