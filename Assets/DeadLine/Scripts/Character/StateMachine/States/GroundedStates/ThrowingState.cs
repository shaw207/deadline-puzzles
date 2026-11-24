using UnityEngine;

namespace DeadLine
{
    public class ThrowingState : GroundedState
    {
        public ThrowingState(Character character) : base(character){}

        protected override void ExitState()
        {
            _character.Render.DeactivateLayerWhenAnimationEnds(AnimationLayers.UpperBody);
            _character.Interaction.Throw();
        }

        protected override void EnterState()
        {
            _character.Render.ChangeAnimation(Animations.Throwing, 0.2f, 0f, AnimationLayers.UpperBody);
        }
    }
}
