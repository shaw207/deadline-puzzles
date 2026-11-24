using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace DeadLine
{
    public class RenderController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator _animator;
        [SerializeField] private SkinnedMeshRenderer _meshRenderer;
        
        private int _currentAnimation;
        private Coroutine _changeAnimationCoroutine;
        private Dictionary<int, float> _originalLayerWeights = new Dictionary<int, float>();
        private Dictionary<int, bool> _activeLayerStates = new Dictionary<int, bool>();

        private const float DEFAULT_CROSSFADE_DURATION = 0.2f;

        
        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();
            
            if (_meshRenderer == null)
                _meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        }

        private void Start()
        {
            if(_animator)
            {
                _animator.applyRootMotion = false;
                
                // Guardar pesos originales de todas las layers
                for (int i = 0; i < _animator.layerCount; i++)
                {
                    _originalLayerWeights[i] = _animator.GetLayerWeight(i);
                    _activeLayerStates[i] = i == 0; // Solo la base layer está activa por defecto
                    
                    // Desactivar layers que no sean la base
                    if (i > 0)
                        _animator.SetLayerWeight(i, 0f);
                }
            }
        }

        /// <summary>
        /// Reproduce una animación específica
        /// </summary>
        /// <param name="animation"></param>
        public void PlayAnimation(int animation)
        {
            if(!_animator) return;
            if(_changeAnimationCoroutine != null) StopCoroutine(_changeAnimationCoroutine);
    
            _currentAnimation = animation; 
            _animator.speed = 1f;
            _animator.Play(animation);
        }

        /// <summary>
        /// Detiene la animación actual
        /// </summary>
        public void StopAnimation()
        {
            if(!_animator) return;
            if(_changeAnimationCoroutine != null) StopCoroutine(_changeAnimationCoroutine);

            _currentAnimation = -1;
            _animator.speed = 0f;
        }

        /// <summary>
        /// Cambia a una nueva animación con crossfade
        /// </summary>
        public void ChangeAnimation(int animation, float crossfade = DEFAULT_CROSSFADE_DURATION, float fixedTimeOffset = 0f, string layer = AnimationLayers.Base)
        {
            if(!_animator || animation == _currentAnimation) return;
            if(_changeAnimationCoroutine != null) StopCoroutine(_changeAnimationCoroutine);

            _currentAnimation = animation;
            int layerIndex = _animator.GetLayerIndex(layer);
            
            // Activar la layer si no es la base
            if (layerIndex > 0)
            {
                SetLayerActive(layerIndex, true);
            }
            
            _animator.CrossFadeInFixedTime(animation, crossfade, layerIndex, fixedTimeOffset);
            _animator.speed = 1f;
        }

        /// <summary>
        /// Cambia a una nueva animación cuando la actual termine
        /// </summary>
        public void ChangeAnimationWhenEnds(int animation, float crossfade = DEFAULT_CROSSFADE_DURATION, float fixedTimeOffset = 0f, string layer = AnimationLayers.Base)
        {
            if(!_animator || animation == _currentAnimation) return;
            
            AnimatorStateInfo animStateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            float remainingTime = animStateInfo.length * (1f - (animStateInfo.normalizedTime % 1f));

            ChangeAnimationWithDelay(remainingTime, animation, crossfade, fixedTimeOffset, layer);
        }

        /// <summary>
        /// Cambia a una nueva animación después de un tiempo determinado
        /// </summary>
        public void ChangeAnimationWithDelay(float time, int animation, float crossfade = DEFAULT_CROSSFADE_DURATION, float fixedTimeOffset = 0f, string layer = AnimationLayers.Base)
        {
            if(!_animator || animation == _currentAnimation) return;
            if(_changeAnimationCoroutine != null) StopCoroutine(_changeAnimationCoroutine);

            _changeAnimationCoroutine = StartCoroutine(WaitAndChangeAnimation(time, animation, crossfade, fixedTimeOffset, layer));
        }

        /// <summary>
        /// Coroutine para esperar y cambiar la animación
        /// </summary>
        private IEnumerator WaitAndChangeAnimation(float time, int animation, float crossfade = DEFAULT_CROSSFADE_DURATION, float fixedTimeOffset = 0f, string layer = AnimationLayers.Base)
        {
            float remainingTime = time - crossfade;
            if (remainingTime > 0)
                yield return new WaitForSeconds(remainingTime);
            
            _currentAnimation = animation;
            int layerIndex = _animator.GetLayerIndex(layer);    
            _animator.CrossFadeInFixedTime(animation, crossfade, layerIndex, fixedTimeOffset);
            _animator.speed = 1f;

            _changeAnimationCoroutine = null;
        }

        public Animator Animator => _animator;

        /// <summary>
        /// Activa o desactiva una layer del animator
        /// </summary>
        private void SetLayerActive(int layerIndex, bool active)
        {
            if (!_animator || layerIndex < 0 || layerIndex >= _animator.layerCount) return;
            if (layerIndex == 0) return; // No modificar la base layer

            bool wasActive = _activeLayerStates.ContainsKey(layerIndex) && _activeLayerStates[layerIndex];
            
            if (active && !wasActive)
            {
                // Activar layer: restaurar peso original
                float originalWeight = _originalLayerWeights.ContainsKey(layerIndex) ? _originalLayerWeights[layerIndex] : 1f;
                _animator.SetLayerWeight(layerIndex, originalWeight);
                _activeLayerStates[layerIndex] = true;
            }
            else if (!active && wasActive)
            {
                _animator.Play(Animations.Empty, layerIndex);

                // Desactivar layer: poner peso a 0
                _animator.SetLayerWeight(layerIndex, 0f);
                _activeLayerStates[layerIndex] = false;
            }
        }

        /// <summary>
        /// Desactiva una layer cuando la animación actual en esa layer termine
        /// </summary>
        public void DeactivateLayerWhenAnimationEnds(string layerName)
        {
            if (!_animator) return;
            int layerIndex = _animator.GetLayerIndex(layerName);
            if (layerIndex <= 0) return;

            AnimatorStateInfo animStateInfo = _animator.GetCurrentAnimatorStateInfo(layerIndex);
            
            if(_animator.IsInTransition(layerIndex))
            {
                animStateInfo = _animator.GetNextAnimatorStateInfo(layerIndex);
            }

            // Si la animación loopea o normalizedTime > 1, esto calculará el tiempo restante del ciclo actual
            float remainingTime = animStateInfo.length * (1f - (animStateInfo.normalizedTime % 1f));
            
            StartCoroutine(WaitAndDeactivateLayer(remainingTime, layerName));
        }

        private IEnumerator WaitAndDeactivateLayer(float time, string layerName)
        {
            yield return new WaitForSeconds(time);
            DeactivateLayer(layerName);
        }

        /// <summary>
        /// Desactiva una layer específica por nombre
        /// </summary>
        public void DeactivateLayer(string layerName)
        {
            if (!_animator) return;
            int layerIndex = _animator.GetLayerIndex(layerName);
            if (layerIndex > 0)
            {
                SetLayerActive(layerIndex, false);
                
            }
        }

        /// <summary>
        /// Activa una layer específica por nombre
        /// </summary>
        public void ActivateLayer(string layerName)
        {
            if (!_animator) return;
            int layerIndex = _animator.GetLayerIndex(layerName);
            if (layerIndex > 0)
            {
                SetLayerActive(layerIndex, true);
            }
        }

    }

    [System.Serializable]
    /// <summary>
    /// Hashes de animaciones para el Animator
    /// </summary>
    public static class Animations{
        public static readonly int Idle = Animator.StringToHash("Breathing Idle");
        public static readonly int CrouchedIdle = Animator.StringToHash("Crouched");
        public static readonly int WalkingCrouched = Animator.StringToHash("Crouched Walking");
        public static readonly int HardLanding = Animator.StringToHash("Hard Landing");
        public static readonly int SoftLanding = Animator.StringToHash("Soft Landing");
        public static readonly int StartPushing = Animator.StringToHash("Start Pushing");
        public static readonly int Pushing = Animator.StringToHash("Pushing");
        public static readonly int StartPulling = Animator.StringToHash("Start Pulling");
        public static readonly int Pulling = Animator.StringToHash("Pulling");
        public static readonly int HangingLedge = Animator.StringToHash("LedgeIdle");
        public static readonly int ClimbingLedge = Animator.StringToHash("LedgeClimb");
        public static readonly int ClimbingLadderUp = Animator.StringToHash("Climb Up");
        public static readonly int ClimbingLadderDown = Animator.StringToHash("Climb Down");
        public static readonly int Throwing = Animator.StringToHash("Throwing");
        public static readonly int ThrowIdle = Animator.StringToHash("Throw Idle");
        public static readonly int Walking = Animator.StringToHash("Walking");
        public static readonly int Running = Animator.StringToHash("Running");
        public static readonly int JumpStart = Animator.StringToHash("JumpStart");
        public static readonly int Jumping = Animator.StringToHash("Jump");
        public static readonly int Airborn = Animator.StringToHash("Airborn");
        public static readonly int JumpEnd = Animator.StringToHash("JumpEnd");
        public static readonly int Empty = Animator.StringToHash("Empty");
        
    }

    public static class AnimationLayers {
        public const string Base = "Base Layer";
        public const string UpperBody = "UpperBody";
    }
}