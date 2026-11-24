using System.Diagnostics;
using UnityEngine;

namespace DeadLine
{
    public class CrouchIdleState : CrouchState
    {
        public CrouchIdleState(Character character) : base(character){}

        public override void OnEnter()
        {
            _character.Render.ChangeAnimation(Animations.CrouchedIdle);
        }
 
    }
}
