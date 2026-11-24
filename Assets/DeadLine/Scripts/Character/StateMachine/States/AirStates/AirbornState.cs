namespace DeadLine
{
    public class AirborneState : AirState
    {
        public AirborneState(Character character) : base(character){}

        public override void OnEnter()
        {
            _character.Render.ChangeAnimationWhenEnds(Animations.Airborn);
        }
    }
}