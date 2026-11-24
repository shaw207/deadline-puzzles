namespace DeadLine
{
    public class LedgeHangState : ClimbState
    {
        public LedgeHangState(Character character) : base(character){}

        protected override void EnterState()
        {
            _character.Movement.GrabLedge();
            _character.Render.ChangeAnimation(Animations.HangingLedge);    
        }
    }
}
