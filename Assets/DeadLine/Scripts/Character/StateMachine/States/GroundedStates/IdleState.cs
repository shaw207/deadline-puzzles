namespace DeadLine
{
    public class IdleState : GroundedState
    {
        public IdleState(Character character) : base(character){}

        public override void OnEnter()
        {
            _character.Render.ChangeAnimation(Animations.Idle);
        }

    }
}
