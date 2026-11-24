using UnityEngine;
using UnityEngine.Playables;

namespace DeadLine
{
    /// <summary>
    /// Estado principal para escalar escaleras - sigue el patrón de PullingState
    /// </summary>
    public class LadderClimbingState : ClimbState
    {
        private const float EXIT_THRESHOLD = 0.1f;
        
        private PlayableGraph _reverseGraph;
        private bool _isPlayingReverse = false;
        
        public LadderClimbingState(Character character) : base(character) { }

        protected override void EnterState()
        {
            // Iniciar la interacción con la escalera (similar a PullingState)
            if (_character.Interaction.CurrentInteractables != null && 
                _character.Interaction.CurrentInteractables.Length > 0)
            {
                _character.Interaction.StartInteraction(InteractableType.Ladder);
            }
            
            // Montar al personaje en la escalera
            Ladder currentLadder = GetCurrentLadder();
            if (currentLadder != null)
            {
                _character.Movement.MountLadder(currentLadder);
                Debug.Log("[LadderClimbingState] Personaje montado en escalera");
            }
            else
            {
                Debug.LogWarning("[LadderClimbingState] No hay escalera disponible al entrar al estado");
            }
        }

        protected override void HandleInput()
        {
            // Leer solo input vertical para subir/bajar
            Vector2 input = InputManager.Instance.GetMovementInput();

            // Aplicar movimiento de escalada
            _character.Movement.ApplyLadderClimbing(input);
        }

        protected override void ApplyMovement()
        {
            // El movimiento ya se aplicó en HandleInput (dentro de ApplyLadderClimbing)
            // Este método se deja vacío porque MovementController lo maneja todo
            
            // Actualizar la interacción (similar a PullingState)
            _character.Interaction.UpdateInteraction(Vector3.zero);
        }

        protected override void UpdateStateLogic()
        {
            // Actualizar animator con velocidad normalizada
            float normalizedSpeed = _character.Movement.GetNormalizedClimbSpeed();
            
            if(normalizedSpeed < 0f)
            {
                // Bajando - reproducir en reversa
                if (!_isPlayingReverse)
                {
                    // Cambio de dirección: detener animación normal y crear reversa
                    AnimatorReverseHelper.StopAndDestroyGraph(_reverseGraph);
                    _character.Render.ChangeAnimation(Animations.ClimbingLadderUp);
                    _reverseGraph = AnimatorReverseHelper.PlayCurrentAnimatorStateInReverse(_character.Render.Animator, 0);
                    _isPlayingReverse = true;
                }
            }
            else if(normalizedSpeed > 0f)
            {
                // Subiendo - reproducir normal
                if (_isPlayingReverse)
                {
                    // Capturar tiempo normalizado actual antes de detener
                    float currentNormalizedTime = AnimatorReverseHelper.GetNormalizedTime(_reverseGraph);
                    
                    // Cambio de dirección: detener reversa y reproducir normal desde la posición actual
                    AnimatorReverseHelper.StopAndDestroyGraph(_reverseGraph);
                    _isPlayingReverse = false;
                    
                    // Reanudar animación normal desde donde estaba
                    _character.Render.ChangeAnimation(Animations.ClimbingLadderUp);
                    if (currentNormalizedTime >= 0f)
                    {
                        _character.Render.Animator.Play(Animations.ClimbingLadderUp, 0, currentNormalizedTime);
                    }
                }
                else
                {
                    _character.Render.ChangeAnimation(Animations.ClimbingLadderUp);
                }
            }
            else
            {
                // Parado - detener animación
                if (_isPlayingReverse)
                {
                    AnimatorReverseHelper.StopAndDestroyGraph(_reverseGraph);
                    _isPlayingReverse = false;
                }
                _character.Render.StopAnimation();
            }
        }

        protected override void ExitState()
        {
            // Limpiar el graph si existe
            if (_isPlayingReverse)
            {
                AnimatorReverseHelper.StopAndDestroyGraph(_reverseGraph);
                _isPlayingReverse = false;
            }
            
            // Finalizar la interacción con la escalera
            _character.Interaction.EndInteraction();
            
            // Desmontar de la escalera
            _character.Movement.DismountLadder();
            
            Debug.Log("[LadderClimbingState] Personaje desmontado de escalera");
        }

        /// <summary>
        /// Helper para obtener la escalera actual del sistema de interacción
        /// </summary>
        private Ladder GetCurrentLadder()
        {
            if (_character.Interaction.ActiveInteractable is Ladder ladder)
            {
                return ladder;
            }
            
            // Fallback: buscar en los interactables actuales
            if (_character.Interaction.CurrentInteractables != null)
            {
                foreach (var interactable in _character.Interaction.CurrentInteractables)
                {
                    if (interactable is Ladder ladderInteractable)
                        return ladderInteractable;
                }
            }
            
            return null;
        }
    }
}