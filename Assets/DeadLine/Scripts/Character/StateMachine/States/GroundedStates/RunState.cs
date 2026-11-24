namespace DeadLine
{
    public class RunState : GroundedState
    {
        public RunState(Character character) : base(character){}

        public override void OnEnter()
        {
            _character.Render.ChangeAnimation(Animations.Running);
        }

        protected override void ApplyMovement()
        {
            _character.Movement.Run(InputManager.Instance.GetMovementInput());
            _character.Movement.ApplyVerticalMove();
        }
    }
}
