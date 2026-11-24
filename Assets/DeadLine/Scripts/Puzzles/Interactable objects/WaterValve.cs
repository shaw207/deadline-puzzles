using System.Collections;
using UnityEngine;

namespace DeadLine
{
    public class WaterValve : InteractableObjectsBase
    {
        [SerializeField] private Transform rotatingPart;

        [SerializeField] private float fillZ = -90f;
        [SerializeField] private float drainZ = 0f;

        [SerializeField] private float rotateDuration = 0.35f;
        [SerializeField] private float cooldown = 0.25f;

        [SerializeField] private bool playSfx = true;
        [SerializeField] private string sfxOn = "WaterValveOn";
        [SerializeField] private string sfxOff = "WaterValveOff";

        private bool filling = true;
        private bool busy = false;

        public override void Interact()
        {
            if (busy) return;
            if (!checkInteractable()) return;

            StartCoroutine(DoToggle());
        }

        private IEnumerator DoToggle()
        {
            busy = true;
            setInteractable(false);

            if (filling) interpretAction();
            else interpretNoAction();

            if (playSfx)
            {
                string sfx = filling ? sfxOn : sfxOff;
                if (!string.IsNullOrWhiteSpace(sfx))
                    AudioManager.Instance?.PlayOneShotSound(sfx);
            }

            if (rotatingPart != null)
            {
                Vector3 startEuler = rotatingPart.localEulerAngles;
                float startZ = NormalizeSignedZ(startEuler.z);
                float endZ = filling ? fillZ : drainZ;

                float t = 0f;
                float inv = rotateDuration <= 0f ? 0f : 1f / rotateDuration;

                while (t < 1f)
                {
                    t += Time.deltaTime * inv;
                    float z = Mathf.Lerp(startZ, endZ, Mathf.Clamp01(t));
                    rotatingPart.localEulerAngles = new Vector3(startEuler.x, startEuler.y, z);
                    yield return null;
                }

                rotatingPart.localEulerAngles = new Vector3(startEuler.x, startEuler.y, endZ);
            }

            filling = !filling;

            if (cooldown > 0f)
                yield return new WaitForSeconds(cooldown);

            setInteractable(true);
            busy = false;
        }

        private static float NormalizeSignedZ(float z)
        {
            z %= 360f;
            if (z > 180f) z -= 360f;
            if (z < -180f) z += 360f;
            return z;
        }
    }
}
