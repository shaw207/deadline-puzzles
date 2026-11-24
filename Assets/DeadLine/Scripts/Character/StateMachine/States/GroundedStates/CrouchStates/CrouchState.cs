namespace DeadLine
{
    public abstract class CrouchState : GroundedState
    {
        protected CrouchState(Character character) : base(character) {}

        protected override void ApplyMovement()
        {
            _character.Movement.WalkCrouched(InputManager.Instance.GetMovementInput());
            _character.Movement.ApplyVerticalMove();
        }
    }
}
