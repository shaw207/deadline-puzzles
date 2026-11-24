using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DeadLine
{
    public class Player : Character
    {
        protected override void Start()
        {
            base.Start();
            
            var builder = new PlayerStateMachineBuilder(this);
            _stateMachine = builder.Build();

            InputManager.Instance.DebugPressed += () => {CheckpointsManager.Instance.RespawnPlayer(); };
        }

        protected override void Update()
        {
            base.Update();
            _stateMachine.Update();
        }

        private void FixedUpdate()
        {
            _stateMachine.FixedUpdate();
        }

        public void TakeDamage()
        {
            OriginalDemoFlow.Instance.OnPlayerDeath();
        }

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            if (Application.isPlaying && _stateMachine.GetState() != null)
            {
                Vector3 position = new Vector3(transform.position.x, transform.position.y + 1, transform.position.z);
                UnityEditor.Handles.Label(position, "Current State: " + _stateMachine.GetState().GetType().Name);
                Gizmos.color = Color.blue;
            }
 #endif           
        }
    }
}
