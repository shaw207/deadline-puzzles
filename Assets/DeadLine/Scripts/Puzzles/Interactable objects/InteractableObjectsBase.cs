using UnityEngine;

namespace DeadLine
{
    public abstract class InteractableObjectsBase : MonoBehaviour
    {
        [Header("Base")]
        [SerializeField] protected bool active = false;
        [SerializeField] protected bool interactable = true;
        [SerializeField] protected GameObject[] RelativeObjectRefs;

        protected IRelativeObjects[] RelativeObjects;
        private bool _resolved = false;

        [Header("Proximity Prompt")]
        [SerializeField] private bool usePrompt = true;
        [SerializeField] private GameObject promptPrefab;
        [SerializeField] private Transform promptAnchorTransform;

        [Header("Trigger (auto-setup)")]
        [SerializeField] private bool autoEnsureTrigger = false;
        [SerializeField] private float autoTriggerRadius = 1.5f;
        [SerializeField] private string autoTriggerChildName = "__InteractTrigger";

        private ProximityPrompt _promptInstance;
        private Character _characterCached;
        private int _playerOverlapCount = 0;

        private Camera _cachedCam;

        private Transform _autoTriggerTf;
        private SphereCollider _autoTriggerCol;
        private InteractableTriggerForwarder _autoForwarder;

        public abstract void Interact();

        protected virtual void Awake()
        {
            ResolveRelativeObjectsIfNeeded();

            if (promptAnchorTransform == null)
                promptAnchorTransform = transform;

            if (autoEnsureTrigger)
                EnsureAutoTriggerChild();
        }

        protected virtual void OnValidate()
        {
            _resolved = false;

            if (promptAnchorTransform == null)
                promptAnchorTransform = transform;

            if (autoEnsureTrigger)
                EnsureAutoTriggerChild();
        }

        protected virtual void OnDisable()
        {
            HidePrompt();

            if (_characterCached != null)
            {
                var proximityDetector = _characterCached.GetComponentInChildren<ProximityDetector>();
                if (proximityDetector != null)
                    proximityDetector.Unregister(this);

                _characterCached = null;
            }

            _playerOverlapCount = 0;
        }

        protected virtual void OnDestroy()
        {
            OnDisable();
        }

        private void Update()
        {
            if (_characterCached != null && usePrompt)
                UpdatePromptVisibility(false);
        }

        public void NotifyTriggerEnter(Collider other)
        {
            HandleTriggerEnter(other);
        }

        public void NotifyTriggerExit(Collider other)
        {
            HandleTriggerExit(other);
        }

        private void HandleTriggerEnter(Collider other)
        {
            var character = other.GetComponentInParent<Character>();
            if (character == null) return;
            if (character is not Player) return;

            if (_characterCached == null)
            {
                _characterCached = character;
                _playerOverlapCount = 0;
            }

            if (_characterCached != character) return;

            _playerOverlapCount++;
            if (_playerOverlapCount == 1)
            {
                var proximityDetector = character.GetComponentInChildren<ProximityDetector>();
                if (proximityDetector != null)
                    proximityDetector.Register(this);

                UpdatePromptVisibility(true);
            }
        }

        private void HandleTriggerExit(Collider other)
        {
            var character = other.GetComponentInParent<Character>();
            if (character == null) return;
            if (character is not Player) return;
            if (_characterCached != character) return;

            _playerOverlapCount = Mathf.Max(0, _playerOverlapCount - 1);

            if (_playerOverlapCount == 0)
            {
                var proximityDetector = _characterCached.GetComponentInChildren<ProximityDetector>();
                if (proximityDetector != null)
                    proximityDetector.Unregister(this);

                HidePrompt();
                _characterCached = null;
            }
        }

        private void UpdatePromptVisibility(bool forceCreateIfNeeded)
        {
            if (!usePrompt) return;

            bool shouldShow = _characterCached != null && ShouldShowPrompt();

            if (!shouldShow)
            {
                HidePrompt();
                return;
            }

            if (_promptInstance == null)
            {
                if (!forceCreateIfNeeded) return;
                if (promptPrefab == null) return;

                var go = Instantiate(promptPrefab, transform);
                _promptInstance = go.GetComponent<ProximityPrompt>();
                if (_promptInstance == null)
                {
                    Destroy(go);
                    return;
                }
            }

            if (!_promptInstance.gameObject.activeSelf)
                _promptInstance.gameObject.SetActive(true);

            var main = Camera.main;
            if (main != null)
            {
                if (_cachedCam != main)
                    _cachedCam = main;
            }
            else if (_cachedCam == null)
            {
                _cachedCam = Object.FindFirstObjectByType<Camera>();
            }

            var anchor = promptAnchorTransform != null ? promptAnchorTransform : transform;
            _promptInstance.Bind(anchor, _cachedCam);
        }

        private void HidePrompt()
        {
            if (_promptInstance != null)
                _promptInstance.gameObject.SetActive(false);
        }

        protected virtual bool ShouldShowPrompt()
        {
            if (!interactable) return false;
            return CheckStatusForPrompt();
        }

        private bool CheckStatusForPrompt()
        {
            ResolveRelativeObjectsIfNeeded();

            if (!interactable) return false;

            if (RelativeObjects == null || RelativeObjects.Length == 0)
                return true;

            foreach (IRelativeObjects ro in RelativeObjects)
            {
                if (ro == null) continue;
                if (!ro.checkStatu())
                    return false;
            }

            return true;
        }

        private void EnsureAutoTriggerChild()
        {
            _autoTriggerTf = transform.Find(autoTriggerChildName);
            if (_autoTriggerTf == null)
            {
                var child = new GameObject(autoTriggerChildName);
                child.transform.SetParent(transform, false);
                _autoTriggerTf = child.transform;
            }

            _autoTriggerCol = _autoTriggerTf.GetComponent<SphereCollider>();
            if (_autoTriggerCol == null)
                _autoTriggerCol = _autoTriggerTf.gameObject.AddComponent<SphereCollider>();

            _autoTriggerCol.isTrigger = true;
            _autoTriggerCol.radius = Mathf.Max(0.01f, autoTriggerRadius);

            _autoForwarder = _autoTriggerTf.GetComponent<InteractableTriggerForwarder>();
            if (_autoForwarder == null)
                _autoForwarder = _autoTriggerTf.gameObject.AddComponent<InteractableTriggerForwarder>();

            _autoForwarder.SetOwner(this);
        }

        protected void ResolveRelativeObjectsIfNeeded()
        {
            if (_resolved) return;
            _resolved = true;

            if (RelativeObjects != null && RelativeObjects.Length > 0)
                return;

            if (RelativeObjectRefs == null || RelativeObjectRefs.Length == 0)
            {
                RelativeObjects = null;
                return;
            }

            RelativeObjects = new IRelativeObjects[RelativeObjectRefs.Length];

            for (int i = 0; i < RelativeObjectRefs.Length; i++)
            {
                var go = RelativeObjectRefs[i];
                if (go == null) continue;

                var ro = go.GetComponent<IRelativeObjects>();
                if (ro == null) continue;

                RelativeObjects[i] = ro;
            }
        }

        protected bool checkStatus()
        {
            ResolveRelativeObjectsIfNeeded();

            if (!interactable)
                return false;

            if (RelativeObjects == null || RelativeObjects.Length == 0)
                return true;

            foreach (IRelativeObjects ro in RelativeObjects)
            {
                if (ro == null) continue;
                if (!ro.checkStatu())
                    return false;
            }

            return true;
        }

        public bool checkInteractable() => interactable;
        public void setInteractable(bool value) => interactable = value;

        public void setRelativeObjects(IRelativeObjects[] value)
        {
            RelativeObjects = value;
            _resolved = true;
        }

        public IRelativeObjects[] getRelativeObjects()
        {
            ResolveRelativeObjectsIfNeeded();
            return RelativeObjects;
        }

        public void setRelativeObjectRefs(GameObject[] value)
        {
            RelativeObjectRefs = value;
            _resolved = false;
        }

        protected void interpretAction()
        {
            ResolveRelativeObjectsIfNeeded();
            if (RelativeObjects == null || RelativeObjects.Length == 0) return;

            foreach (IRelativeObjects ro in RelativeObjects)
            {
                if (ro == null) continue;

                if (!ro.UseParam()) ro.action();
                else ro.action(this.gameObject);
            }
        }

        protected void interpretNoAction()
        {
            ResolveRelativeObjectsIfNeeded();
            if (RelativeObjects == null || RelativeObjects.Length == 0) return;

            foreach (IRelativeObjects ro in RelativeObjects)
            {
                if (ro == null) continue;

                if (!ro.UseParam()) ro.Noaction();
                else ro.Noaction(this.gameObject);
            }
        }
    }
}
