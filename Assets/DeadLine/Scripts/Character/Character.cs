using System;
using System.Net;
using UnityEngine;

namespace DeadLine
{
    public abstract class Character : MonoBehaviour
    {
        public MovementController Movement => _movementController;
        public InteractionController Interaction => _interactionController;
        public RenderController Render => _renderController;
        public CharacterData Data => _characterData;
        public StateMachine StateMachine => _stateMachine;
        

        [SerializeField] protected MovementController _movementController;
        [SerializeField] protected InteractionController _interactionController;
        [SerializeField] protected RenderController _renderController;
        
        [SerializeField] protected CharacterData _characterData;

        protected StateMachine _stateMachine;


        protected virtual void Awake()
        {
            if (_movementController == null)
                _movementController = GetComponent<MovementController>();

            if (_interactionController == null)
                _interactionController = GetComponent<InteractionController>();

            if (_renderController == null)
                _renderController = GetComponent<RenderController>();
        }

        protected virtual void Start()
        {
            _characterData.InitializeHealth();
        }
        
        protected virtual void Update(){
            _characterData.UpdateHealthRegen();
        }
        
    }

}