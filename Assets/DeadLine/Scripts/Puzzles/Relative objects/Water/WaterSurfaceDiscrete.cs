using UnityEngine;

namespace DeadLine
{
    public class WaterSurfaceDiscrete : MonoBehaviour
    {
        [SerializeField] private Transform surface;
        [SerializeField] private float[] levelHeights;
        [SerializeField] private float moveSpeed = 2f;

        [SerializeField] private bool playSfx = true;
        [SerializeField] private string sfxRising = "HoleBeingFilledWithWater";
        [SerializeField] private string sfxFalling = "MetalWaterDrain";
        [SerializeField] private float minDeltaToPlay = 0.01f;

        private float targetLocalY;

        private void Awake()
        {
            if (surface == null) surface = transform;
            targetLocalY = surface.localPosition.y;
        }

        private void Update()
        {
            var p = surface.localPosition;
            p.y = Mathf.MoveTowards(p.y, targetLocalY, moveSpeed * Time.deltaTime);
            surface.localPosition = p;
        }

        public void SetLevel(int level)
        {
            if (levelHeights == null || levelHeights.Length == 0) return;
            level = Mathf.Clamp(level, 0, levelHeights.Length - 1);

            float nextY = levelHeights[level];
            float curTarget = targetLocalY;

            if (playSfx && AudioManager.Instance != null)
            {
                float delta = nextY - curTarget;
                if (Mathf.Abs(delta) >= minDeltaToPlay)
                {
                    string sfx = delta > 0f ? sfxRising : sfxFalling;
                    if (!string.IsNullOrWhiteSpace(sfx))
                        AudioManager.Instance.PlaySoundAtPoint(sfx, surface.position);
                }
            }

            targetLocalY = nextY;
        }
    }
}