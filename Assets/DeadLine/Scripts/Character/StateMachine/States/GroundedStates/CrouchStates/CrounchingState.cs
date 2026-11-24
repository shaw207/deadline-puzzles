using System.Diagnostics;
using UnityEngine;

namespace DeadLine
{
    public class CrouchingState : CrouchState
    {
        public CrouchingState(Character character) : base(character){}

        protected override void EnterState()
        {
            _character.Movement.Crouch();
            _character.Render.ChangeAnimation(Animations.CrouchedIdle);
        }
    }
}
