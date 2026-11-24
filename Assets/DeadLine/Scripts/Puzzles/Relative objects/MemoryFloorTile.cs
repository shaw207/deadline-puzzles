using System.Collections;
using UnityEngine;

namespace DeadLine
{
    [RequireComponent(typeof(Collider))]
    public class MemoryFloorTile : MonoBehaviour
    {
        private MemoryFloorPuzzleController _controller;

        [Header("Visual Flash (Material Swap)")]
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Material flashMaterial;
        [SerializeField] private bool useSharedMaterial = false;

        private Material _original;
        private Coroutine _flashRoutine;

        public void Register(MemoryFloorPuzzleController controller) => _controller = controller;

        private void Awake()
        {
            if (targetRenderer == null)
                targetRenderer = GetComponentInChildren<Renderer>();

            if (targetRenderer != null)
                _original = targetRenderer.sharedMaterial;
        }

        private void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            _controller?.OnTileStepped(this);
        }

        public void Flash(float duration, int times = 1)
        {
            if (targetRenderer == null || flashMaterial == null) return;

            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashRoutine(duration, Mathf.Max(1, times)));
        }

        private IEnumerator FlashRoutine(float duration, int times)
        {
            for (int i = 0; i < times; i++)
            {
                ApplyMat(flashMaterial);
                yield return new WaitForSeconds(duration * 0.5f);

                ApplyMat(_original);
                yield return new WaitForSeconds(duration * 0.5f);
            }

            ApplyMat(_original);
            _flashRoutine = null;
        }

        private void ApplyMat(Material mat)
        {
            if (useSharedMaterial) targetRenderer.sharedMaterial = mat;
            else targetRenderer.material = mat;
        }
    }
}
