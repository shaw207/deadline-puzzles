using UnityEngine;

namespace DeadLine
{
    public class WaterLevelController : MonoBehaviour, IRelativeObjects
    {
        [SerializeField] private Transform bottomRef;
        [SerializeField] private Transform topRef;
        [SerializeField] private float speed = 1.2f;

        private float currentY;
        private int direction;

        public float CurrentSurfaceY => currentY;
        public float BottomY => bottomRef.position.y;

        [SerializeField] private bool startFull = true;

        [SerializeField] private bool playSfx = true;
        [SerializeField] private string sfxRising = "HoleBeingFilledWithWater";
        [SerializeField] private string sfxFalling = "MetalWaterDrain";

        private bool _isMoving;
        private int _moveDir;

        private void Start()
        {
            currentY = startFull ? topRef.position.y : bottomRef.position.y;
        }

        private void Update()
        {
            if (direction == 0) return;

            if (playSfx && !_isMoving)
            {
                _isMoving = true;
                _moveDir = direction;

                string sfx = _moveDir > 0 ? sfxRising : sfxFalling;
                if (!string.IsNullOrWhiteSpace(sfx))
                    AudioManager.Instance?.PlaySoundAtPoint(sfx, transform.position);
            }

            float target = direction > 0 ? topRef.position.y : bottomRef.position.y;
            currentY = Mathf.MoveTowards(currentY, target, speed * Time.deltaTime);

            if (Mathf.Approximately(currentY, target))
            {
                direction = 0;
                _isMoving = false;
                _moveDir = 0;
            }
        }

        public void action() => direction = 1;
        public void action(object obj) => direction = 1;
        public void Noaction() => direction = -1;
        public void Noaction(object obj) => direction = -1;
        public bool checkStatu() => true;
        public bool UseParam() => false;
    }
}