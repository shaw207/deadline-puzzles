using System.Collections;
using UnityEngine;

namespace DeadLine
{
    [RequireComponent(typeof(Collider))]
    public class WaterDrownTrigger : MonoBehaviour
    {
        [SerializeField] private float drownDelay = 2.0f;

        [SerializeField] private float deathFadeOutDuration = 0.35f;
        [SerializeField] private float deathFadeInDuration = 0.35f;

        [SerializeField] private string playerTag = "Player";
        [SerializeField] private bool requireFullyInside = true;

        private Collider waterCol;

        private float insideTime;
        private bool drowning;
        private Coroutine drownRoutine;

        private void Awake()
        {
            waterCol = GetComponent<Collider>();
            waterCol.isTrigger = true;
        }

        private void OnTriggerStay(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;

            if (requireFullyInside && !IsFullyInside(other, waterCol))
            {
                StopDrown();
                return;
            }

            insideTime += Time.deltaTime;

            if (!drowning && insideTime >= drownDelay)
            {
                drowning = true;
                drownRoutine = StartCoroutine(DoDeathSequence());
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            StopDrown();
        }

        private void StopDrown()
        {
            insideTime = 0f;
            drowning = false;

            if (drownRoutine != null)
            {
                StopCoroutine(drownRoutine);
                drownRoutine = null;
            }
        }

        private IEnumerator DoDeathSequence()
        {
            OriginalDemoFlow.Instance.FadeOut(deathFadeOutDuration);
            yield return new WaitForSeconds(deathFadeOutDuration);
            OriginalDemoFlow.Instance.OnPlayerDeath();
            OriginalDemoFlow.Instance.FadeIn(deathFadeInDuration);

            yield return new WaitForSeconds(deathFadeInDuration);

            StopDrown();
        }

        private static bool IsFullyInside(Collider inner, Collider outer)
        {
            Bounds ib = inner.bounds;
            Bounds ob = outer.bounds;
            return ob.Contains(ib.min) && ob.Contains(ib.max);
        }
    }
}
