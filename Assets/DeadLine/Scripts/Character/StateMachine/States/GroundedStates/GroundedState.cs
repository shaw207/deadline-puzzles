using UnityEngine;

namespace DeadLine
{
    public abstract class GroundedState : BaseState
    {
        private bool _wasInteractPressed = false;

        protected GroundedState(Character character) : base(character) {}

        protected override void ApplyPhysics()
        {
            _character.Movement.ApplyGroundedGravity();
        }

        protected override void ApplyMovement()
        {
            _character.Movement.Walk(InputManager.Instance.GetMovementInput());
            _character.Movement.ApplyVerticalMove();
            _character.Interaction.DetectInteractables();
        }

        protected override void UpdateStateLogic()
        {
            // Manejar interacciones por proximidad
            bool isInteractPressed = InputManager.Instance.IsInteractPressed;
            if (isInteractPressed && !_wasInteractPressed)
            {
                _character.Interaction.HandleProximityInteraction();
            }
            
            _wasInteractPressed = isInteractPressed;
        }
    }
}