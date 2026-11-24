namespace DeadLine
{
    public class StandingUpState : GroundedState
    {
        public StandingUpState(Character character) : base(character){}

        protected override void EnterState()
        {
            _character.Movement.StandUp();
        }
    }
}
