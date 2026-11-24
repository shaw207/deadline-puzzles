using System.Collections;
using UnityEngine;

namespace DeadLine
{
    public class BotonPerm : InteractableObjectsBase
    {
        [Header("Animation Settings")] [SerializeField]
        private Transform buttonVisual;

        [Tooltip("Dirección local en la que se mueve el botón al pulsarse")] [SerializeField]
        private Vector3 pressDirection = Vector3.down;

        [Tooltip("Distancia que recorre el botón al pulsarse")] [SerializeField]
        private float pressDistance = 0.05f;

        [SerializeField] private float pressSpeed = 10f;

        [Header("Cooldown (CD)")] [SerializeField]
        private float cooldownSeconds = 1.0f;

        [SerializeField] private bool autoResetAfterCD = true;

        [Header("SFX")]
        [SerializeField] private string sfxPress = "Button_Click";
        [SerializeField] private string sfxDenied = "Button_Denied";
        [SerializeField] private string sfxReset = "Button_Reset";

        private bool _onCooldown = false;

        private Vector3 _initialPos;
        private Vector3 _pressedPos;

        private bool _isAnimating = false;
        private Coroutine _cooldownRoutine;

        protected override void Awake()
        {
            if (buttonVisual == null)
                buttonVisual = this.transform;

            _initialPos = buttonVisual.localPosition;

            Vector3 dir = pressDirection;
            if (dir == Vector3.zero)
                dir = Vector3.down;

            dir.Normalize();

            _pressedPos = _initialPos + dir * pressDistance;
        }

        public override void Interact()
        {
            if (_onCooldown)
            {
                if (!string.IsNullOrWhiteSpace(sfxDenied))
                    AudioManager.Instance?.PlayOneShotSound(sfxDenied);
                return;
            }

            if (!checkStatus())
            {
                if (!string.IsNullOrWhiteSpace(sfxDenied))
                    AudioManager.Instance?.PlayOneShotSound(sfxDenied);
                return;
            }

            if (!string.IsNullOrWhiteSpace(sfxPress))
                AudioManager.Instance?.PlayOneShotSound(sfxPress);

            active = true;
            setInteractable(false);

            StartPressAnimation();
            interpretAction();

            StartCooldown();
        }

        public void reset()
        {
            if (_cooldownRoutine != null)
            {
                StopCoroutine(_cooldownRoutine);
                _cooldownRoutine = null;
            }

            _onCooldown = false;
            setInteractable(true);
            active = false;

            if (!string.IsNullOrWhiteSpace(sfxReset))
                AudioManager.Instance?.PlayOneShotSound(sfxReset);

            StartResetAnimation();
        }

        private void StartCooldown()
        {
            _onCooldown = true;

            if (_cooldownRoutine != null)
                StopCoroutine(_cooldownRoutine);

            _cooldownRoutine = StartCoroutine(CooldownRoutine());
        }

        private IEnumerator CooldownRoutine()
        {
            if (cooldownSeconds > 0f)
                yield return new WaitForSeconds(cooldownSeconds);

            _onCooldown = false;
            setInteractable(true);

            if (autoResetAfterCD)
            {
                active = false;

                if (!string.IsNullOrWhiteSpace(sfxReset))
                    AudioManager.Instance?.PlayOneShotSound(sfxReset);

                StartResetAnimation();
            }

            _cooldownRoutine = null;
        }

        private void StartPressAnimation()
        {
            if (_isAnimating) return;
            StartCoroutine(PressRoutine());
        }

        private IEnumerator PressRoutine()
        {
            _isAnimating = true;

            float t = 0f;
            Vector3 start = buttonVisual.localPosition;

            while (t < 1f)
            {
                t += Time.deltaTime * pressSpeed;
                buttonVisual.localPosition = Vector3.Lerp(start, _pressedPos, t);
                yield return null;
            }

            buttonVisual.localPosition = _pressedPos;
            _isAnimating = false;
        }

        private void StartResetAnimation()
        {
            if (_isAnimating) return;
            StartCoroutine(ResetRoutine());
        }

        private IEnumerator ResetRoutine()
        {
            _isAnimating = true;

            float t = 0f;
            Vector3 start = buttonVisual.localPosition;

            while (t < 1f)
            {
                t += Time.deltaTime * pressSpeed;
                buttonVisual.localPosition = Vector3.Lerp(start, _initialPos, t);
                yield return null;
            }

            buttonVisual.localPosition = _initialPos;
            _isAnimating = false;
        }
    }
}
