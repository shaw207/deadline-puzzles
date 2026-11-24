using System.Collections;
using UnityEngine;

namespace DeadLine
{
    public class AutoDoor : InteractableObjectsBase, IRelativeObjects
    {
        [SerializeField] private Transform door;

        [Header("Rotation")]
        [SerializeField] private float openAngleY = 90f;
        [SerializeField] private float openDuration = 0.6f;

        [Header("Auto Trigger")]
        [SerializeField] private bool ensureDoorTrigger = true;
        [SerializeField] private float triggerRadius = 2.0f;

        [Header("SFX")]
        [SerializeField] private bool playDoorSfx = true;
        [SerializeField] private string sfxOpen = "DoorOpenCreak";

        private bool _opened;
        private bool _busy;
        private Quaternion _closedRotation;

        private Character _characterCached;
        private int _overlapCount;

        protected override void Awake()
        {
            if (door == null)
                door = transform;

            _closedRotation = door.localRotation;

            if (ensureDoorTrigger)
                EnsureTriggerCollider();
        }

        private void EnsureTriggerCollider()
        {

            Collider col = GetComponent<SphereCollider>();
            if (col == null) col = GetComponent<BoxCollider>();
            if (col == null) col = GetComponent<CapsuleCollider>();

            if (col == null)
            {
                var sc = gameObject.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = triggerRadius;
            }
            else
            {
                col.isTrigger = true;

                if (col is SphereCollider s) s.radius = triggerRadius;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var character = other.GetComponentInParent<Character>();
            if (character == null) return;

            // Solo procesar si es el jugador (Player)
            if (character is not Player) return;

            if (_characterCached == null)
            {
                _characterCached = character;
                _overlapCount = 0;
            }

            if (_characterCached != character) return;

            _overlapCount++;
            if (_overlapCount == 1)
                Interact(); 
        }

        private void OnTriggerExit(Collider other)
        {
            var character = other.GetComponentInParent<Character>();
            if (character == null) return;
            
            if (character is not Player) return;
            
            if (_characterCached != character) return;

            _overlapCount = Mathf.Max(0, _overlapCount - 1);
            if (_overlapCount == 0)
                _characterCached = null;
        }

        public override void Interact()
        {
            if (_busy) return;
            if (_opened) return;
            if (!checkInteractable()) return;

            if (playDoorSfx && !string.IsNullOrWhiteSpace(sfxOpen))
                AudioManager.Instance?.PlayOneShotSound(sfxOpen);

            StartCoroutine(OpenOnce());
        }

        private IEnumerator OpenOnce()
        {
            _busy = true;
            setInteractable(false);

            interpretAction();

            Quaternion start = door.localRotation;
            Quaternion end = _closedRotation * Quaternion.Euler(0f, openAngleY, 0f);

            if (openDuration <= 0f)
            {
                door.localRotation = end;
                _opened = true;
                _busy = false;
                yield break;
            }

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / openDuration;
                door.localRotation = Quaternion.Slerp(start, end, Mathf.Clamp01(t));
                yield return null;
            }

            door.localRotation = end;
            _opened = true;
            _busy = false;
        }

        public void action() => Interact();
        public void action(object obj) => action();

        public void Noaction() { }
        public void Noaction(object obj) { }

        public bool checkStatu() => _opened;
        public bool UseParam() => false;
    }
}
