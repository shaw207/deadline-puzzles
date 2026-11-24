using System.Collections;
using UnityEngine;

namespace DeadLine
{
    public class LightPedestal : MonoBehaviour
    {
        [SerializeField] private int requiredSymbol;
        [SerializeField] private Transform reflectPoint;
        [SerializeField] private LightBeamController controller;

        [SerializeField] private float snapDuration = 0.35f;
        [SerializeField] private float pushForce = 6f;
        [SerializeField] private float upForce = 2f;

        [SerializeField] private bool playSfx = true;
        [SerializeField] private string sfxSnap = "AltarGlow";

        private bool solved;
        private Coroutine running;

        public bool Solved => solved;
        public Transform ReflectPoint => reflectPoint != null ? reflectPoint : transform;

        private void OnTriggerEnter(Collider other)
        {
            if (solved) return;

            var crystal = other.GetComponentInParent<LightCrystal>();
            if (crystal == null) return;
            if (running != null) return;

            if (crystal.SymbolId == requiredSymbol)
                running = StartCoroutine(SnapCrystal(crystal));
            else
                RejectCrystal(crystal);
        }

        private IEnumerator SnapCrystal(LightCrystal crystal)
        {
            solved = true;
            controller?.RebuildBeam();

            if (playSfx && !string.IsNullOrWhiteSpace(sfxSnap))
                AudioManager.Instance?.PlayOneShotSound(sfxSnap);

            var rb = crystal.RB;
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.useGravity = false;
                rb.isKinematic = true;
            }

            Transform t = crystal.transform;
            Vector3 startPos = t.position;
            Quaternion startRot = t.rotation;

            Vector3 endPos = ReflectPoint.position + new Vector3(0f, -1f, 0f);
            Quaternion endRot = ReflectPoint.rotation * Quaternion.Euler(180f, 0f, 0f);

            float a = 0f;
            float inv = snapDuration > 0f ? 1f / snapDuration : 0f;

            while (a < 1f)
            {
                a += Time.deltaTime * inv;
                float k = Mathf.Clamp01(a);
                t.position = Vector3.Lerp(startPos, endPos, k);
                t.rotation = Quaternion.Slerp(startRot, endRot, k);
                yield return null;
            }

            t.position = endPos;
            t.rotation = endRot;
            running = null;
        }

        private void RejectCrystal(LightCrystal crystal)
        {
            var rb = crystal.RB;
            if (rb == null) return;

            rb.isKinematic = false;
            rb.useGravity = true;

            Vector3 dir = (crystal.transform.position - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) dir = transform.forward;
            dir.Normalize();

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(dir * pushForce + Vector3.up * upForce, ForceMode.VelocityChange);
        }
    }
}
