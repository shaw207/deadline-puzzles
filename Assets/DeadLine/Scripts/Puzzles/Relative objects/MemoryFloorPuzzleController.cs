using System.Collections;
using UnityEngine;

namespace DeadLine
{
    public class MemoryFloorPuzzleController : MonoBehaviour, IRelativeObjects
    {
        [Header("Todas las baldosas del puzzle (correctas + falsas)")]
        [SerializeField] private MemoryFloorTile[] allTiles;

        [Header("Baldosas en el orden correcto")]
        [SerializeField] private MemoryFloorTile[] correctSequence;

        [Header("Si el puzzle NO está activo, ¿pisar falla?")]
        [SerializeField] private bool failWhenNotActive = true;

        [Header("Puerta a abrir cuando se resuelva")]
        [SerializeField] private GameObject relativeDoor;

        [Header("Pista al activar (Flash secuencia)")]
        [SerializeField] private bool showSequenceOnActivate = true;
        [SerializeField] private float startDelay = 0.10f;
        [SerializeField] private float flashDuration = 0.25f;
        [SerializeField] private float intervalBetweenTiles = 0.10f;
        [SerializeField] private int flashTimesPerTile = 1;

        private IRelativeObjects _doorTarget;
        private int _currentIndex = 0;
        private bool _active = false;
        private bool _solved = false;

        private Coroutine _showRoutine;

        private void Awake()
        {
            // 1) Auto-rellenar allTiles si no lo asignas en Inspector
            if (allTiles == null || allTiles.Length == 0)
                allTiles = GetComponentsInChildren<MemoryFloorTile>(true);

            // 2) Registrar TODAS las baldosas (correctas + falsas)
            if (allTiles != null)
            {
                foreach (var tile in allTiles)
                    if (tile != null) tile.Register(this);
            }

            // Puerta
            if (relativeDoor != null)
            {
                _doorTarget = relativeDoor.GetComponent<IRelativeObjects>();
                if (_doorTarget == null)
                    Debug.LogWarning(relativeDoor.name + " no implementa IRelativeObjects");
            }
        }

        public void OnTileStepped(MemoryFloorTile tile)
        {
            if (_solved) return;

            // Si NO está activo, y quieres que "no se pueda pisar" => falla igual
            if (!_active)
            {
                if (failWhenNotActive) OnPuzzleFailed();
                return;
            }

            if (correctSequence == null || correctSequence.Length == 0) return;

            // Orden correcto o fallo
            if (_currentIndex < correctSequence.Length && correctSequence[_currentIndex] == tile)
            {
                _currentIndex++;
                if (_currentIndex >= correctSequence.Length)
                    OnPuzzleSolved();
            }
            else
            {
                OnPuzzleFailed();
            }
        }

        private void OnPuzzleSolved()
        {
            _solved = true;
            _active = false;

            if (_doorTarget != null) _doorTarget.action();
        }

        private void OnPuzzleFailed()
        {
            _currentIndex = 0;

            OriginalDemoFlow.Instance?.OnPlayerDeath();
        }

        // ===== IRelativeObjects =====

        public void action()
        {
            _solved = false;
            _currentIndex = 0;

            // Si ya estaba mostrando secuencia, reiniciamos
            if (_showRoutine != null)
            {
                StopCoroutine(_showRoutine);
                _showRoutine = null;
            }

            if (showSequenceOnActivate)
            {
                // Bloquea pisadas mientras se muestra la pista
                _active = false;
                _showRoutine = StartCoroutine(ShowSequenceThenActivate());
            }
            else
            {
                _active = true;
            }
        }

        private IEnumerator ShowSequenceThenActivate()
        {
            if (startDelay > 0f)
                yield return new WaitForSeconds(startDelay);

            if (correctSequence == null || correctSequence.Length == 0)
            {
                Debug.LogWarning($"{name}: correctSequence vacía, no hay nada que flashear.", this);
                _active = true;
                _showRoutine = null;
                yield break;
            }

            for (int i = 0; i < correctSequence.Length; i++)
            {
                var tile = correctSequence[i];
                if (tile == null) continue;

                // AQUÍ SE LLAMA A FLASH
                tile.Flash(flashDuration, flashTimesPerTile);

                yield return new WaitForSeconds(flashDuration + intervalBetweenTiles);
            }

            // Ahora sí: puzzle activo
            _active = true;
            _showRoutine = null;
        }

        public void Noaction()
        {
            _active = false;

            if (_showRoutine != null)
            {
                StopCoroutine(_showRoutine);
                _showRoutine = null;
            }
        }

        public void action(object obj) => action();
        public void Noaction(object obj) => Noaction();
        public bool checkStatu() => true;
        public bool UseParam() => false;
    }
}
