namespace DeadLine
{
    public class WalkState : GroundedState
    {
        public WalkState(Character character) : base(character){}

        public override void OnEnter()
        {
            _character.Render.ChangeAnimation(Animations.Walking);
        }
    }
}
