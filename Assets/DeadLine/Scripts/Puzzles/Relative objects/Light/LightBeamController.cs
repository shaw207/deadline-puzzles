using System.Collections.Generic;
using UnityEngine;

namespace DeadLine
{
    public class LightBeamController : MonoBehaviour, IRelativeObjects
    {
        [SerializeField] private Transform sourcePoint;
        [SerializeField] private LightPedestal[] pedestals;

        [SerializeField] private LineRenderer beam;
        [SerializeField] private float maxDistance = 50f;

        [Header("Door")]
        [SerializeField] private GameObject doorObject;   
        [SerializeField] private Transform doorLightEndPoint;    
        [SerializeField] private bool autoOpenOnComplete = true;

        private IRelativeObjects _door;
        private bool _opened;

        private void Awake()
        {
            if (doorObject != null)
                _door = doorObject.GetComponent<IRelativeObjects>();
        }

        private void Start()
        {
            RebuildBeam();
        }

        public void RebuildBeam()
        {
            if (beam == null || sourcePoint == null)
            {
                if (beam != null) beam.positionCount = 0;
                return;
            }

            var pts = new List<Vector3>(32);

            Vector3 origin = sourcePoint.position;
            pts.Add(origin);

            for (int i = 0; i < pedestals.Length; i++)
            {
                var p = pedestals[i];
                if (p == null) break;

                Vector3 target = p.ReflectPoint.position;
                Vector3 dir = (target - origin).normalized;

                if (!p.Solved)
                {
                    pts.Add(origin + dir * maxDistance);
                    Apply(pts);
                    return;
                }

                pts.Add(target);
                origin = target;
            }

            if (doorLightEndPoint != null)
                pts.Add(doorLightEndPoint.position);
            else
                pts.Add(origin + (origin - sourcePoint.position).normalized * maxDistance);

            Apply(pts);

            if (autoOpenOnComplete && IsCompleted && !_opened)
                TryOpenDoor();
        }

        private void Apply(List<Vector3> pts)
        {
            beam.positionCount = pts.Count;
            for (int i = 0; i < pts.Count; i++)
                beam.SetPosition(i, pts[i]);
        }

        private void TryOpenDoor()
        {
            if (_door == null) return;
            if (!_door.checkStatu()) return;

            _door.action();
            _opened = true;
        }

        public bool IsCompleted
        {
            get
            {
                if (pedestals == null || pedestals.Length == 0) return false;
                foreach (var p in pedestals)
                    if (p == null || !p.Solved) return false;
                return true;
            }
        }

        public void action()
        {
            if (!IsCompleted) return;
            TryOpenDoor();
        }

        public void action(object obj) => action();

        public void Noaction()
        {
            if (_door != null && _door.checkStatu())
                _door.Noaction();
        }

        public void Noaction(object obj) => Noaction();

        public bool checkStatu() => IsCompleted;

        public bool UseParam() => false;
    }
}
