namespace DeadLine
{
    public class PushingState : GroundedState
    {
        public PushingState(Character character) : base(character){}

        protected override void EnterState()
        {
            if (_character.Interaction.CurrentInteractables != null && _character.Interaction.CurrentInteractables.Length > 0)
                _character.Interaction.StartInteraction(InteractableType.Push);
            _character.Render.ChangeAnimation(Animations.StartPushing, 0.1f, 0f, AnimationLayers.UpperBody);
        }


        protected override void ApplyMovement()
        {
            _character.Movement.WalkWhileMovingObject(InputManager.Instance.GetMovementInput(), _character.Interaction.ActiveInteractable.Weight);
            _character.Movement.ApplyVerticalMove();
            _character.Interaction.DetectInteractables();
            _character.Interaction.UpdateInteraction(InputManager.Instance.GetMovementInput());
        }

        protected override void UpdateStateLogic()
        {
            base.UpdateStateLogic();
            _character.Render.ChangeAnimationWhenEnds(Animations.Pushing, 0.2f, 0f, AnimationLayers.UpperBody);
        }

        protected override void ExitState()
        {
            _character.Render.DeactivateLayer(AnimationLayers.UpperBody);

            if (_character.Interaction.IsInteracting)
                _character.Interaction.EndInteraction();
        }
    }
}
