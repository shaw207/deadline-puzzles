using System.Collections;
using UnityEngine;

namespace DeadLine
{
    public class WaterValvePuzzle3 : InteractableObjectsBase
    {
        [SerializeField] private WaterPuzzleController controller;

        [SerializeField] private int deltaA = 0;
        [SerializeField] private int deltaB = 0;
        [SerializeField] private int deltaC = 0;

        [SerializeField] private Transform rotatingPart;
        [SerializeField] private float rotateZ = -90f;
        [SerializeField] private float rotateDuration = 0.35f;
        [SerializeField] private float cooldown = 0.25f;

        private bool busy;
        private float _baseZ;
        private bool _baseCaptured;

        public override void Interact()
        {
            if (busy) return;
            if (!checkInteractable()) return;
            if (controller == null) return;

            StartCoroutine(DoValve());
        }

        private IEnumerator DoValve()
        {
            busy = true;
            setInteractable(false);

            if (rotatingPart != null)
            {
                Vector3 startEuler = rotatingPart.localEulerAngles;
                float startZ = NormalizeSignedAngle(startEuler.z);

                float targetZ = startZ + rotateZ;

                float t = 0f;
                float inv = rotateDuration > 0f ? 1f / rotateDuration : 0f;

                while (t < 1f)
                {
                    t += Time.deltaTime * inv;
                    float z = Mathf.Lerp(startZ, targetZ, Mathf.Clamp01(t));
                    rotatingPart.localEulerAngles = new Vector3(startEuler.x, startEuler.y, z);
                    yield return null;
                }

                rotatingPart.localEulerAngles = new Vector3(startEuler.x, startEuler.y, targetZ);
            }


            controller.ApplyValveDelta(deltaA, deltaB, deltaC);

            if (cooldown > 0f)
                yield return new WaitForSeconds(cooldown);

            setInteractable(true);
            busy = false;
        }

        private static float NormalizeSignedAngle(float a)
        {
            a %= 360f;
            if (a > 180f) a -= 360f;
            if (a < -180f) a += 360f;
            return a;
        }
    }
}
