using UnityEngine;

namespace DeadLine
{
    public class JumpState : AirState
    {
        public JumpState(Character character) : base(character){}

        protected override void EnterState()
        {
            _character.Movement.Jump();
            _character.Render.PlayAnimation(Animations.JumpStart);
        }

        protected override void ApplyPhysics()
        {
            _character.Movement.ApplyJumpGravity();
        }
    }
}
