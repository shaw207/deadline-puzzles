namespace DeadLine
{
    public abstract class AirState : BaseState
    {
        protected AirState(Character character) : base(character) {}

        protected override void ApplyPhysics()
        {
            _character.Movement.ApplyAirborneGravity();
        }

        protected override void ApplyMovement()
        {
            _character.Movement.MoveInAir(InputManager.Instance.GetMovementInput());
            _character.Movement.ApplyVerticalMove();
        }
    }
}