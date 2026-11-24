using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DeadLine
{
    public class CheckpointsManager : MonoBehaviour
    {
        public static CheckpointsManager Instance { get; private set; }

        [Header("Checkpoints")]
        [SerializeField] private List<Checkpoint> checkpoints = new List<Checkpoint>();
        
        [Header("Settings")]
        [SerializeField] private Transform player;
        
        private Checkpoint _currentCheckpoint;
        private Checkpoint _lastCheckpoint;
        
        // Eventos
        public event Action<Checkpoint> OnCheckpointReached;
        public event Action<Vector3> OnPlayerRespawn;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            FindAllCheckpoints();
            SortCheckpoints();
        }

        private void Start()
        {
            if (player == null)
                player = GameObject.FindGameObjectWithTag("Player")?.transform;

            // Activar el primer checkpoint como inicial
            if (checkpoints.Count > 0)
            {
                _currentCheckpoint = checkpoints[0];
                _lastCheckpoint = _currentCheckpoint;
            }
        }

        private void FindAllCheckpoints()
        {
            checkpoints = FindObjectsByType<Checkpoint>(FindObjectsSortMode.None).ToList();
        }

        private void SortCheckpoints()
        {
            checkpoints = checkpoints.OrderBy(c => c.Order).ToList();
        }

        public void ReachCheckpoint(Checkpoint checkpoint)
        {
            if (checkpoint == null) return;

            if (_currentCheckpoint == null || checkpoint.Order >= _currentCheckpoint.Order)
            {
                _lastCheckpoint = _currentCheckpoint;
                _currentCheckpoint = checkpoint;
                
                checkpoint.Activate();
                
                OnCheckpointReached?.Invoke(checkpoint);
            }
        }

        public void RespawnPlayer()
        {
            if (_currentCheckpoint == null)
            {
                return;
            }

            Vector3 respawnPosition = _currentCheckpoint.GetRespawnPosition();
            GameObject respawnVisual = _currentCheckpoint.GetRespawnVisual();
            if (respawnVisual != null)
            {
                Instantiate(respawnVisual, respawnPosition, Quaternion.identity);
            }
            
            if (player != null)
            {
                player.position = respawnPosition;
                OnPlayerRespawn?.Invoke(respawnPosition);
            }

        }

        public Checkpoint GetCurrentCheckpoint() => _currentCheckpoint;

        public Checkpoint GetLastCheckpoint() => _lastCheckpoint;

        public List<Checkpoint> GetAllCheckpoints() => checkpoints;

        public void ResetAllCheckpoints()
        {
            foreach (var checkpoint in checkpoints)
            {
                checkpoint.Deactivate();
            }

            if (checkpoints.Count > 0)
            {
                _currentCheckpoint = checkpoints[0];
                _lastCheckpoint = _currentCheckpoint;
                _currentCheckpoint.Activate();
            }
        }

        public void TeleportToNextCheckpoint()
        {
            if (checkpoints.Count == 0) return;

            if (_currentCheckpoint == null)
            {
                ReachCheckpoint(checkpoints[0]);
                RespawnPlayer();
                return;
            }

            int currentIndex = checkpoints.IndexOf(_currentCheckpoint);
            if (currentIndex >= 0 && currentIndex < checkpoints.Count - 1)
            {
                Checkpoint nextCheckpoint = checkpoints[currentIndex + 1];
                ReachCheckpoint(nextCheckpoint);
                RespawnPlayer();
            }
        }

        public void TeleportToPreviousCheckpoint()
        {
            if (checkpoints.Count == 0) return;

            if (_currentCheckpoint == null)
            {
                ReachCheckpoint(checkpoints[0]);
                RespawnPlayer();
                return;
            }

            int currentIndex = checkpoints.IndexOf(_currentCheckpoint);
            if (currentIndex > 0)
            {
                Checkpoint previousCheckpoint = checkpoints[currentIndex - 1];
                ReachCheckpoint(previousCheckpoint);
                RespawnPlayer();
            }
        }

        private void OnDrawGizmos()
        {
            if (checkpoints == null || checkpoints.Count == 0) return;

            for (int i = 0; i < checkpoints.Count - 1; i++)
            {
                if (checkpoints[i] != null && checkpoints[i + 1] != null)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(
                        checkpoints[i].transform.position,
                        checkpoints[i + 1].transform.position
                    );
                }
            }
        }

        
    }
}
