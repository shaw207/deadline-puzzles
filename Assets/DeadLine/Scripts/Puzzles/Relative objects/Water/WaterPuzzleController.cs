using UnityEngine;
using UnityEngine.Events;

namespace DeadLine
{
    public class WaterPuzzleController : MonoBehaviour
    {
        [Header("Max Levels")]
        [SerializeField] private int maxA = 1;
        [SerializeField] private int maxB = 2;
        [SerializeField] private int maxC = 3;

        [Header("Current Levels (Read Only)")]
        [SerializeField] private int levelA = 0;
        [SerializeField] private int levelB = 0;
        [SerializeField] private int levelC = 0;

        [Header("Visual")]
        [SerializeField] private WaterSurfaceDiscrete visualA;
        [SerializeField] private WaterSurfaceDiscrete visualB;
        [SerializeField] private WaterSurfaceDiscrete visualC;

        [Header("Solved Events")]
        [SerializeField] private bool triggerOnce = true;
        [SerializeField] private UnityEvent onSolved;
        [SerializeField] private UnityEvent onUnsolved;

        private bool _wasSolved;
        private bool _triggeredOnce;

        private void Start()
        {
            ApplyVisuals();
            _wasSolved = IsSolved();

            if (_wasSolved && !triggerOnce)
            {
                onSolved?.Invoke();
            }
            else if (_wasSolved && triggerOnce && !_triggeredOnce)
            {
                _triggeredOnce = true;
                onSolved?.Invoke();
            }
        }

        public void ApplyValveDelta(int deltaA, int deltaB, int deltaC)
        {
            levelA = AddWithWrap(levelA, deltaA, maxA);
            levelB = AddWithWrap(levelB, deltaB, maxB);
            levelC = AddWithWrap(levelC, deltaC, maxC);

            ApplyVisuals();
            EvaluateSolvedState();
        }

        public void SetLevels(int a, int b, int c)
        {
            levelA = Mathf.Clamp(a, 0, maxA);
            levelB = Mathf.Clamp(b, 0, maxB);
            levelC = Mathf.Clamp(c, 0, maxC);

            ApplyVisuals();
            EvaluateSolvedState();
        }

        private void EvaluateSolvedState()
        {
            bool solvedNow = IsSolved();


            if (!_wasSolved && solvedNow)
            {
                if (!triggerOnce || !_triggeredOnce)
                {
                    _triggeredOnce = true;
                    onSolved?.Invoke();
                }
            }

            else if (_wasSolved && !solvedNow)
            {
                onUnsolved?.Invoke();

            }

            _wasSolved = solvedNow;
        }

        private static int AddWithWrap(int cur, int add, int max)
        {
            int m = max + 1;
            int next = (cur + add) % m;
            if (next < 0) next += m;
            return next;
        }

        private void ApplyVisuals()
        {
            if (visualA != null) visualA.SetLevel(levelA);
            if (visualB != null) visualB.SetLevel(levelB);
            if (visualC != null) visualC.SetLevel(levelC);
        }

        public bool IsSolved()
        {
            return levelA == maxA && levelB == maxB && levelC == maxC;
        }
    }
}
