using UnityEngine;
using UnityEngine.Splines;

namespace DeadLine
{
    [RequireComponent(typeof(Collider))]
    public class CameraSplineTrigger : MonoBehaviour
    {
        [Header("Use existing SplineContainer")]
        [SerializeField] private SplineContainer spline;

        [Header("Build spline from points")]
        [SerializeField] private GameObject[] controlPoints;
        [SerializeField] private bool buildOnAwake = true;
        [SerializeField] private bool useWorldSpace = true;
        [SerializeField] private bool closed = false;

        [Header("Auto-create container if needed")]
        [SerializeField] private bool createContainerOnThisObject = true;

        private void Awake()
        {
            if (buildOnAwake)
                EnsureSplineBuilt();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            EnsureSplineBuilt();
            if (spline == null) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            CameraController camLogic = cam.GetComponent<CameraController>();
            if (camLogic == null) return;

            camLogic.SetSplineMode(spline);
        }

        private void EnsureSplineBuilt()
        {
            if (spline != null) return;

            if (controlPoints == null || controlPoints.Length < 2)
                return;

            if (createContainerOnThisObject)
            {
                spline = GetComponent<SplineContainer>();
                if (spline == null) spline = gameObject.AddComponent<SplineContainer>();
            }
            else
            {
                var child = new GameObject("RuntimeSplineContainer");
                child.transform.SetParent(transform, false);
                spline = child.AddComponent<SplineContainer>();
            }

            var newSpline = new Spline();
            for (int i = 0; i < controlPoints.Length; i++)
            {
                var go = controlPoints[i];
                if (go == null) continue;

                Vector3 p = go.transform.position;
                if (!useWorldSpace)
                    p = transform.InverseTransformPoint(p);

                newSpline.Add(new BezierKnot(p));
            }

            if (newSpline.Count < 2)
                return;

            newSpline.Closed = closed;

            for (int i = spline.Splines.Count - 1; i >= 0; i--)
                spline.RemoveSplineAt(i);

            spline.AddSpline(newSpline);
        }
    }
}
