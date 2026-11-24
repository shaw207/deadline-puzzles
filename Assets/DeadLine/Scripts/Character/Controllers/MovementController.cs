using System;
using System.Collections;
using UnityEngine;

namespace DeadLine
{
    public class MovementController : MonoBehaviour
    {
        #region "Inspector Variables"

        [Header("Options")]
        [SerializeField] private bool _freeLookCamMovement = false;
        [SerializeField] private Transform _cameraTransform;

        [Header("Movement Data")]
        [SerializeField] private MovementData _movementData;
        [SerializeField] private GroundDetector _groundDetector;
        [SerializeField] private CeilingDetector _ceilingDetector;
        [SerializeField] private CollisionPrevention _collisionPreventionDetector;
        [SerializeField] private LedgeDetector _ledgeDetector;


        #endregion

        #region "Private Variables"

        private Character _character;
        private Vector2 _currentMovementInput;
        private Vector3 _currentMovementValue;
        private Vector3 _currentMovement 
        { 
            get => _currentMovementValue;
            set 
            { 
                float clampedY = Mathf.Max(value.y, MathUtils.Negative(_movementData.MaxFallSpeed));
                _currentMovementValue = new Vector3(value.x, clampedY, value.z);
            }
        }
        private Vector3 _horizontalVelocity;

        private float _currentHeight;
        private Coroutine _changeHeightCoroutine;

        private float _jumpGravity => -2f * _movementData.MaxJumpHeight / Mathf.Pow(_movementData.TimeToApex, 2);
        private float _fallgravity => _jumpGravity * _movementData.FallGravityMultiply;
        private float _jumpStartHorizontalSpeed;
        private bool _wasRepositioned = false;
        
        private bool _isGrounded => _groundDetector.CheckCollision();
        private bool _ledgeDetected => _ledgeDetector.CheckCollision();
        private bool _canGrabLedge = true;
        private bool _isAlreadyCrouched = false;
        private bool _isAlreadyStandUp = true;
        
        private bool _isClimbingStep = false;
        private float _stepClimbProgress = 0f;
        private float _stepClimbDuration = 0.2f;
        private float _stepTargetHeight = 0f;
        private float _stepStartY = 0f;

        private Ladder _currentLadder = null;
        private bool _isOnLadder = false;
        private Vector3 _ladderPosition;
        private Vector2 _currentClimbVelocity = Vector2.zero;

        #endregion

        private void Awake()
        {
            _character = GetComponent<Character>();
        
            _currentHeight = _movementData.Height;

            _ledgeDetector.Character = _character;
            _groundDetector.Character = _character;
            _collisionPreventionDetector.Character = _character;

            if (_cameraTransform == null)
                _cameraTransform = Camera.main?.transform;
        }

        #region "Movement Methods"

        /// <summary>
        /// Aplica el movimiento vertical basado en la velocidad actual
        /// </summary>
        public void ApplyVerticalMove()
        {
            _character.transform.position += new Vector3(0, _currentMovement.y * Time.deltaTime, 0);
        }
    
        /// <summary>
        /// Maneja la rotación del personaje basada en la dirección del movimiento
        /// </summary>
        public void HandleRotation()
        {
            Vector3 movementDirection = new Vector3(_currentMovement.x, 0, _currentMovement.z);
            
            if (movementDirection.sqrMagnitude < 0.01f) return;
            
            Vector3 targetDirection = movementDirection.normalized;
            
            Quaternion currentRotation = _character.transform.rotation;
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

            Rotate(Quaternion.Slerp(currentRotation, targetRotation, 15f * Time.deltaTime));
        }
        
        /// <summary>
        /// Corre el personaje a una velocidad dada basada en el input
        /// </summary>
        public void Run(Vector2 input)
        {
            ApplyHorizontalMove(input.normalized, _movementData.MaxRunSpeed, _movementData.RunningAcceleration, _movementData.RunningDeceleration);
        }

        /// <summary>
        /// Detiene el movimiento horizontal del personaje
        /// </summary>
        public void StopHorizontalMove()
        {
            _horizontalVelocity = Vector3.zero;
            _currentMovementInput = Vector2.zero;
        }

        /// <summary>
        /// Mueve al personaje caminando a una velocidad dada basada en el input
        /// </summary>
        public void Walk(Vector2 input)
        {
            ApplyHorizontalMove(input.normalized, _movementData.MaxWalkSpeed, _movementData.Acceleration, _movementData.Deceleration);
        }

        /// <summary>
        /// Mueve al personaje caminando agachado a una velocidad dada basada en el input
        /// </summary>
        public void WalkCrouched(Vector2 input)
        {
            ApplyHorizontalMove(input.normalized, _movementData.MaxCrouchSpeed, _movementData.CrouchAcceleration, _movementData.CrouchDeceleration);
        }
        /// <summary>
        /// Mueve al personaje caminando mientras sostiene un objeto pesado
        /// </summary>
        public void WalkWhileMovingObject(Vector2 input, Weight weight)
        {
            ApplyHorizontalMove(input.normalized, _movementData.MaxWalkSpeed * (1 - weight.GetValue()), _movementData.Acceleration * (1 - weight.GetValue()), _movementData.Deceleration * (1 - weight.GetValue()));
        }

         /// <summary>
        /// Aplica el movimiento horizontal basado en el input y las velocidades dadas
        /// </summary>
        private void ApplyHorizontalMove(Vector2 input, float speed, float acceleration, float deceleration)
        {
            _currentMovementInput = input;
            
            // Calcular dirección del input
            Vector3 inputDirection;
            
            if (_freeLookCamMovement && _cameraTransform != null)
            {
                Vector3 forward = _cameraTransform.forward;
                Vector3 right = _cameraTransform.right;
                
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();
                
                inputDirection = forward * _currentMovementInput.y + right * _currentMovementInput.x;
            }
            else
            {
                inputDirection = new Vector3(_currentMovementInput.x, 0, _currentMovementInput.y);
            }
            
            inputDirection = Vector3.ClampMagnitude(inputDirection, 1f);

            _currentMovement = new Vector3(inputDirection.x, _currentMovement.y, inputDirection.z);
            
            float targetSpeed = speed;

            if(!_isGrounded && _jumpStartHorizontalSpeed > targetSpeed)
                targetSpeed = _jumpStartHorizontalSpeed;

            Vector3 targetHorizontalVelocity = inputDirection * targetSpeed;

            if(inputDirection.sqrMagnitude > 0.0001f)
                _horizontalVelocity = Vector3.MoveTowards(
                    _horizontalVelocity,
                    targetHorizontalVelocity,
                    acceleration * Time.deltaTime
                );
            else
                _horizontalVelocity = Vector3.MoveTowards(
                    _horizontalVelocity,
                    Vector3.zero,
                    deceleration * Time.deltaTime
                );

            Vector3 intendedMovement = _horizontalVelocity * Time.deltaTime;
            Vector3 validMovement = _collisionPreventionDetector.GetValidMovement(
                _character.transform.position,
                intendedMovement,
                _currentHeight
            );

            validMovement = new Vector3(validMovement.x, 0, validMovement.z);
            
            _character.transform.position += validMovement;
            
            // Detectar escalones si está en el suelo y moviéndose
            if (_isGrounded && !_isClimbingStep && intendedMovement.sqrMagnitude > 0.0001f)
            {
                Vector3 moveDir = intendedMovement.normalized;
                float stepHeight = DetectStepHeight(moveDir);
                
                if (stepHeight > 0.05f && stepHeight <= _movementData.MaxStepHeight)
                {
                    StartClimbingStep(stepHeight);
                }
                else if (stepHeight < -0.05f && stepHeight >= -_movementData.MaxStepHeight)
                {
                    StartClimbingStep(stepHeight);
                }
            }

            if (_isClimbingStep)
            {
                UpdateStepClimbing();
            }

            if (!_collisionPreventionDetector.IsPositionValid(transform.position, _currentHeight))
            {
                Debug.LogWarning("[Movement] Posición inválida detectada! Corrigiendo...");
                
                Vector3 nearestValidPosition = _collisionPreventionDetector.GetNearestValidPosition(transform.position, _currentHeight);
                _character.transform.position = Vector3.Lerp(_character.transform.position, nearestValidPosition, 0.5f);
                _wasRepositioned = true;
            }
            else
            {
                _wasRepositioned = false;
            }
        }

        /// <summary>
        /// Rota el personaje a una rotación específica
        /// </summary>
        private void Rotate(Quaternion rotation)
        {
            _character.transform.rotation = rotation;
        }


        #endregion

        #region "Crouch Methods"

        /// <summary>
        /// Agacha al personaje
        /// </summary>
        public void Crouch()
        {
            if(_changeHeightCoroutine != null) StopCoroutine(_changeHeightCoroutine);
            _changeHeightCoroutine = StartCoroutine(CrouchCoroutine());        
        }

        /// <summary>
        /// Hace que el personaje se levante de estar agachado
        /// </summary>
        public void StandUp()
        {
            if(_ceilingDetector.CheckCollision()) return;
            if(_changeHeightCoroutine != null) StopCoroutine(_changeHeightCoroutine);
            
            _changeHeightCoroutine = StartCoroutine(StandUpCoroutine());    
        }

        private IEnumerator CrouchCoroutine()
        {
            _isAlreadyStandUp = false;

            while (_currentHeight > _movementData.CrouchHeight)
            {
                _currentHeight = Mathf.MoveTowards(_currentHeight, _movementData.CrouchHeight, _movementData.ToCrouchingSpeed * Time.deltaTime);
                yield return null;
            }
            _currentHeight = _movementData.CrouchHeight;
            
            _isAlreadyCrouched = true;
            _changeHeightCoroutine = null;
        }

        private IEnumerator StandUpCoroutine()
        {
            _isAlreadyCrouched = false; 

            while (_currentHeight < _movementData.Height)
            {
                _currentHeight = Mathf.MoveTowards(_currentHeight, _movementData.Height, _movementData.ToCrouchingSpeed * Time.deltaTime);
                yield return null;
            }
            _currentHeight = _movementData.Height; 
            
            _isAlreadyStandUp = true;
            _changeHeightCoroutine = null;
        }

        #endregion

        #region "Jump Methods"

        /// <summary>
        /// Aplica la gravedad durante un salto
        /// </summary>
        public void ApplyJumpGravity()
        {            
            float previousVerticalVelocity = _currentMovement.y;
            float newVerticalVelocity = _currentMovement.y + MathUtils.Negative(_jumpGravity) * Time.deltaTime;
            float nextVerticalVelocity = (previousVerticalVelocity + newVerticalVelocity) * 0.5f;
            
            _currentMovement = new Vector3(_currentMovement.x, nextVerticalVelocity, _currentMovement.z);
        }

        /// <summary>
        /// Inicia un salto
        /// </summary>
        public void Jump()
        {
            float initialJumpVelocity = 2 * _movementData.MaxJumpHeight / _movementData.TimeToApex;
            _jumpStartHorizontalSpeed = new Vector2(_horizontalVelocity.x, _horizontalVelocity.z).magnitude; 
            _currentMovement = new Vector3(_horizontalVelocity.x, initialJumpVelocity * 0.5f, _horizontalVelocity.z);
        }

        #endregion

        #region "Air Movement Methods"
        
        /// <summary>
        /// Aplica la gravedad cuando el personaje está en el aire
        /// </summary>
        public void ApplyAirborneGravity()
        {
            float previousVerticalVelocity = _currentMovement.y;
            float newVerticalVelocity = _currentMovement.y + MathUtils.Negative(_fallgravity) * Time.deltaTime;
            float nextVerticalVelocity = (previousVerticalVelocity + newVerticalVelocity) * 0.5f;
            
            _currentMovement = new Vector3(_currentMovement.x, nextVerticalVelocity, _currentMovement.z);
        }
        /// <summary>
        /// Aplica la gravedad cuando el personaje está en el suelo
        /// </summary>
        public void ApplyGroundedGravity()
        {
            var feet = _character.transform.position;
            // A falling fixed step may end just inside the floor before GroundDetector
            // changes state. Resolve that shallow vertical contact before the horizontal
            // capsule checks walls. Keep the authored movement and step limits.
            float probe = _movementData.MaxStepHeight + _movementData.SkinWidth;
            if (_groundDetector.CheckCollision() &&
                Physics.Raycast(feet + Vector3.up * probe, Vector3.down, out var support,
                    probe + _movementData.SkinWidth, _movementData.GroundLayer, QueryTriggerInteraction.Ignore) &&
                support.normal.y >= .70f && support.point.y > feet.y + .001f)
            {
                float lift = support.point.y - feet.y;
                if (lift <= _movementData.MaxStepHeight)
                    _character.transform.position = feet + Vector3.up * (lift + .002f);
            }

            _currentMovement = new Vector3(_horizontalVelocity.x, 0f, _horizontalVelocity.z);
        }

        /// <summary>
        /// Mueve al personaje en el aire basado en el input
        /// </summary>
        public void MoveInAir(Vector2 input)
        {
            ApplyHorizontalMove(input.normalized, _movementData.MaxAirSpeed, _movementData.AirAcceleration, _movementData.AirDeceleration);
        }

        #endregion

        #region "Ledge Methods"
        
        /// <summary>
        /// Hace que el personaje escale el saliente detectado
        /// </summary>
        public void ClimbLedge()
        {
            _character.transform.position = _ledgeDetector.LedgePosition;
            _canGrabLedge = true;
        }

        /// <summary>
        /// Hace que el personaje agarre el saliente detectado
        /// </summary>
        public void GrabLedge()
        {
            _canGrabLedge = false;
            _currentMovement = new Vector3(_currentMovement.x, 0, _currentMovement.z);
        }

        #endregion

        #region "Step Climbing Methods"
        
        /// <summary>
        /// Detecta si hay un escalón adelante y devuelve su altura
        /// </summary>
        private float DetectStepHeight(Vector3 moveDirection)
        {
            Vector3 characterPosition = _character.transform.position;
            float capsuleRadius = _movementData.CharacterRadius;
            float detectionDistance = _movementData.StepDetectionDistance + capsuleRadius;
            
            Vector3 lowRayStart = characterPosition + Vector3.up * 0.1f;
            
            if (Physics.Raycast(lowRayStart, moveDirection, out RaycastHit frontHit, 
                detectionDistance, _movementData.GroundLayer))
            {
                for (float checkHeight = 0.15f; checkHeight <= _movementData.MaxStepHeight; checkHeight += 0.1f)
                {
                    Vector3 upperStart = characterPosition + Vector3.up * checkHeight;
                    
                    if (!Physics.Raycast(upperStart, moveDirection, detectionDistance, _movementData.GroundLayer))
                    {
                        Vector3 stepCheckPos = frontHit.point + moveDirection * 0.15f + Vector3.up * checkHeight;
                        
                        if (Physics.Raycast(stepCheckPos, Vector3.down, out RaycastHit stepGroundHit, 
                            checkHeight + 0.2f, _movementData.GroundLayer))
                        {
                            float stepHeight = stepGroundHit.point.y - characterPosition.y;
                            
                            if (stepHeight > 0.05f && stepHeight <= _movementData.MaxStepHeight)
                            {
                                Vector3 headCheck = stepGroundHit.point + Vector3.up * _currentHeight;
                                if (!Physics.CheckSphere(headCheck, capsuleRadius * 0.5f, _movementData.GroundLayer))
                                {
                                    return stepHeight;
                                }
                            }
                        }
                        break;
                    }
                }
            }
            
            Vector3 forwardCheckPos = characterPosition + moveDirection * (capsuleRadius + 0.3f);
            Vector3 downRayStart = forwardCheckPos + Vector3.up * 0.1f;
            
            if (!Physics.Raycast(downRayStart, Vector3.down, 0.15f, _movementData.GroundLayer))
            {
                if (Physics.Raycast(downRayStart, Vector3.down, out RaycastHit downHit, 
                    _movementData.MaxStepHeight + 0.2f, _movementData.GroundLayer))
                {
                    float stepHeight = downHit.point.y - characterPosition.y;
                    
                    if (stepHeight < -0.05f && stepHeight >= -_movementData.MaxStepHeight)
                    {
                        return stepHeight;
                    }
                }
            }
            
            return 0f;
        }

        /// <summary>
        /// Inicia la "animación" de escalar un escalón
        /// </summary>
        private void StartClimbingStep(float stepHeight)
        {
            _isClimbingStep = true;
            _stepClimbProgress = 0f;
            _stepTargetHeight = stepHeight;
            _stepStartY = _character.transform.position.y;
        }

        /// <summary>
        /// Actualiza la "animación" de escalar un escalón
        /// </summary>
        private void UpdateStepClimbing()
        {
            _stepClimbProgress += Time.deltaTime / _stepClimbDuration;
            
            if (_stepClimbProgress >= 1f)
            {
                Vector3 finalPos = _character.transform.position;
                finalPos.y = _stepStartY + _stepTargetHeight;
                _character.transform.position = finalPos;
                
                _isClimbingStep = false;
                _stepClimbProgress = 0f;
            }
            else
            {
                float t = _stepTargetHeight > 0 
                    ? 1f - Mathf.Pow(1f - _stepClimbProgress, 3f) 
                    : Mathf.Pow(_stepClimbProgress, 2f); 
                
                float currentHeightOffset = Mathf.Lerp(0f, _stepTargetHeight, t);
                
                Vector3 newPos = _character.transform.position;
                newPos.y = _stepStartY + currentHeightOffset;
                _character.transform.position = newPos;
            }
        }

        #endregion

        #region "Ladder Methods"

        /// <summary>
        /// Montar en la escalera - posiciona al personaje y lo prepara para escalar
        /// </summary>
        public void MountLadder(Ladder ladder)
        {
            if (ladder == null)
            {
                Debug.LogWarning("[MovementController] Intentando montar escalera nula");
                return;
            }

            _currentLadder = ladder;
            _isOnLadder = true;

            // Obtener posición de montaje más cercana
            Vector3 mountPosition = ladder.GetNearestMountPosition(_character.transform.position);
            
            // Guardar posición X,Z fija en la escalera (Y se mueve libremente)
            _ladderPosition = new Vector3(mountPosition.x, 0f, mountPosition.z);
            
            // Ajustar posición X,Z del personaje a la escalera
            Vector3 newPos = _character.transform.position;
            newPos.x = mountPosition.x;
            newPos.z = mountPosition.z;
            _character.transform.position = newPos;

            // Rotar hacia la escalera
            Vector3 facingDirection = ladder.GetFacingDirection();
            _character.transform.rotation = Quaternion.LookRotation(facingDirection);

            // Resetear velocidades
            _currentMovement = Vector3.zero;
            _horizontalVelocity = Vector3.zero;
            _currentClimbVelocity = Vector2.zero;

            Debug.Log($"[MovementController] Montado en escalera en posición Y: {newPos.y}");
        }

        /// <summary>
        /// Aplicar movimiento de escalada con input vertical
        /// </summary>
        public void ApplyLadderClimbing(Vector2 input)
        {
            if (!_isOnLadder || _currentLadder == null) return;

            // Calcular velocidad objetivo basada en input
            Vector2 targetVelocity = input * _movementData.LadderClimbSpeed;

            // Aplicar aceleración suave
            _currentClimbVelocity = Vector2.MoveTowards(
                _currentClimbVelocity,
                targetVelocity,
                _movementData.LadderClimbAcceleration * Time.deltaTime
            );

            // Calcular nueva posición Y (movimiento vertical)
            float newY = _character.transform.position.y + _currentClimbVelocity.y * Time.deltaTime;

            // Limitar a los bounds de la escalera
            newY = _currentLadder.ClampToLadderBounds(newY);

            // Calcular movimiento lateral usando el eje "right" de la escalera
            Vector3 lateralMovement = -_currentLadder.transform.right * _currentClimbVelocity.x * Time.deltaTime;

            // Aplicar posición (base + movimiento lateral acumulado)
            Vector3 newPosition = _character.transform.position + lateralMovement;
            newPosition.y = newY;
            
            // Clampear la posición final a los límites laterales
            newPosition = _currentLadder.ClampToLateralBounds(newPosition);
            
            _character.transform.position = newPosition;
        }

        /// <summary>
        /// Desmontar de la escalera - limpia el estado
        /// </summary>
        public void DismountLadder()
        {
            _isOnLadder = false;
            _character.transform.position = _currentLadder.GetNearestDismountPosition(_character.transform.position);
            _currentLadder = null;
            _currentClimbVelocity = Vector2.zero;
            Debug.Log("[MovementController] Desmontado de escalera");
        }

        /// <summary>
        /// Velocidad normalizada de escalada para animaciones (-1 bajando, 0 idle, 1 subiendo)
        /// </summary>
        public float GetNormalizedClimbSpeed()
        {
            if (_movementData.LadderClimbSpeed == 0f) return 0f;
            return Mathf.Clamp(_currentClimbVelocity.y / _movementData.LadderClimbSpeed, -1f, 1f);
        }

        /// <summary>
        /// Verifica si el personaje ha llegado al tope de la escalera
        /// </summary>
        public bool HasReachedLadderTop()
        {
            if (!_isOnLadder || _currentLadder == null) return false;

            float currentY = _character.transform.position.y;
            float topY = _currentLadder.TopMountPoint.position.y;
            
            return currentY >= topY - 0.1f;
        }

        /// <summary>
        /// Verifica si el personaje ha llegado al fondo de la escalera
        /// </summary>
        public bool HasReachedLadderBottom()
        {
            if (!_isOnLadder || _currentLadder == null) return false;

            float currentY = _character.transform.position.y;
            float bottomY = _currentLadder.BottomMountPoint.position.y;
            
            return currentY <= bottomY + 0.1f;
        }

        /// <summary>
        /// Verifica si llegó a cualquier extremo de la escalera
        /// </summary>
        public bool HasReachedLadderEnd()
        {
            return HasReachedLadderTop() || HasReachedLadderBottom();
        }

        /// <summary>
        /// Teleportar al punto de desmontaje superior (para salir por arriba)
        /// </summary>
        public void ExitLadderTop()
        {
            if (_currentLadder == null) return;
            
            Vector3 exitPos = _currentLadder.GetTopDismountPosition();
            _character.transform.position = exitPos;
            
            Debug.Log($"[MovementController] Saliendo por arriba de la escalera a: {exitPos}");
        }

        /// <summary>
        /// Teleportar al punto de desmontaje inferior (para salir por abajo)
        /// </summary>
        public void ExitLadderBottom()
        {
            if (_currentLadder == null) return;
            
            Vector3 exitPos = _currentLadder.GetBottomDismountPosition();
            _character.transform.position = exitPos;
            
            Debug.Log($"[MovementController] Saliendo por abajo de la escalera a: {exitPos}");
        }

        #endregion

        #region "Public Properties"

        public MovementData MovementData => _movementData;
        public GroundDetector GroundDetector => _groundDetector;
        public Ladder CurrentLadder => _currentLadder;  
        public bool CanGrabLedge => _ledgeDetected && _canGrabLedge;
        public bool CanStandUp => !_ceilingDetector.CheckCollision();    
        public bool IsFalling => _currentMovement.y < (_movementData.GroundedGravity * -1);
        public bool IsGrounded => _isGrounded;
        public bool IsAlreadyCrouched => _isAlreadyCrouched;
        public bool IsAlreadyStandUp => _isAlreadyStandUp;
        public bool WasRepositioned => _wasRepositioned;
        public bool IsMoving => _currentMovementInput.sqrMagnitude > 0.01f;
        public bool IsOnLadder => _isOnLadder;
        public float CurrentHeight => _currentHeight;
        public float HorizontalSpeed => new Vector2(_horizontalVelocity.x, _horizontalVelocity.z).magnitude;
        public float VerticalVelocity => _currentMovement.y;
        public float NormalizedHorizontalSpeed => HorizontalSpeed / _movementData.MaxOverallSpeed;
        public Vector3 CurrentFacingDirection => _character.transform.forward;

        #endregion

    }
}