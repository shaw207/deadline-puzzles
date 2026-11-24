using UnityEngine;

namespace DeadLine
{
    public class LedgeClimbingState : ClimbState
    {
        public LedgeClimbingState(Character character) : base(character){}

        protected override void EnterState()
        {
            _character.Movement.ClimbLedge();
            InputManager.Instance.IsJumpPressed = false;
        }
    }
}
