namespace DeadLine
{
    public class CrouchWalkState : CrouchState
    {
        public CrouchWalkState(Character character) : base(character) {}

        public override void OnEnter()
        {
            base.OnEnter();
            _character.Render.ChangeAnimation(Animations.WalkingCrouched);
        }
    }
}
