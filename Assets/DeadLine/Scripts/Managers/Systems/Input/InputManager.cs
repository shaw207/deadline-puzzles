using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DeadLine
{
    public class InputManager : MonoBehaviour
    {
        [SerializeField] private bool enableDebugControls = true;
        public static InputManager Instance;

        private PlayerInputActions _inputActions;

        private bool _isMovementPressed, _isJumpPressed, _isCrouchPressed, _isSprintPressed, _isPausePressed, _isInteractPressed;

        public Action CrouchPressed, CrouchCanceled, DebugPressed, InteractPressed, InteractCanceled, ThrowPressed, ThrowCanceled;

        public Action PausePressed;

        public Action UiEscPressed;
        public Action UiEnterPressed;
        public Action UiSpacePressed;
        public Action UiTabPressed;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                // The standalone scenes each carry their original input and sound configuration.
                _inputActions = new PlayerInputActions();
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void OnValidate()
        {
            if (_inputActions == null)
                return;

            if (enableDebugControls)
                EnableDebugInputActions();
            else
                DisableDebugInputActions();
        }

        private void OnEnable()
        {
            if (_inputActions == null)
                return;

            _inputActions.Player.Move.started += OnMoveStarted;
            _inputActions.Player.Move.canceled += OnMoveCanceled;

            _inputActions.Player.Crouch.started += OnCrouchStarted;
            _inputActions.Player.Crouch.canceled += OnCrouchCanceled;

            _inputActions.Player.Interact.started += OnInteractStarted;
            _inputActions.Player.Interact.canceled += OnInteractCanceled;

            _inputActions.Player.Debug.started += OnDebugStarted;

            _inputActions.Player.Jump.started += OnJumpStarted;
            _inputActions.Player.Jump.canceled += OnJumpCanceled;

            _inputActions.Player.Sprint.started += OnSprintStarted;
            _inputActions.Player.Sprint.canceled += OnSprintCanceled;

            _inputActions.Player.Throw.started += OnThrowStarted;
            _inputActions.Player.Throw.canceled += OnThrowCanceled;

            _inputActions.Player.Pause.performed += OnPause;

            _inputActions.Ui.Esc.performed += OnUiEsc;
            _inputActions.Ui.Enter.performed += OnUiEnter;
            _inputActions.Ui.Space.performed += OnUiSpace;
            _inputActions.Ui.Tab.performed += OnUiTab;

            _inputActions.Debug.NextCheckpoint.performed += ctx => CheckpointsManager.Instance.TeleportToNextCheckpoint();
            _inputActions.Debug.PreviousCheckpoint.performed += ctx => CheckpointsManager.Instance.TeleportToPreviousCheckpoint();

            EnablePlayerInputActions();
            if (enableDebugControls)
                EnableDebugInputActions();
            else
                DisableDebugInputActions();
        }

        private void OnDisable()
        {
            if (_inputActions == null)
                return;

            _inputActions.Player.Move.started -= OnMoveStarted;
            _inputActions.Player.Move.canceled -= OnMoveCanceled;

            _inputActions.Player.Crouch.started -= OnCrouchStarted;
            _inputActions.Player.Crouch.canceled -= OnCrouchCanceled;

            _inputActions.Player.Interact.started -= OnInteractStarted;
            _inputActions.Player.Interact.canceled -= OnInteractCanceled;

            _inputActions.Player.Debug.started -= OnDebugStarted;

            _inputActions.Player.Jump.started -= OnJumpStarted;
            _inputActions.Player.Jump.canceled -= OnJumpCanceled;

            _inputActions.Player.Sprint.started -= OnSprintStarted;
            _inputActions.Player.Sprint.canceled -= OnSprintCanceled;

            _inputActions.Player.Throw.started -= OnThrowStarted;
            _inputActions.Player.Throw.canceled -= OnThrowCanceled;

            _inputActions.Player.Pause.performed -= OnPause;

            _inputActions.Ui.Esc.performed -= OnUiEsc;
            _inputActions.Ui.Enter.performed -= OnUiEnter;
            _inputActions.Ui.Space.performed -= OnUiSpace;
            _inputActions.Ui.Tab.performed -= OnUiTab;

            _inputActions.Debug.NextCheckpoint.performed -= ctx => CheckpointsManager.Instance.TeleportToNextCheckpoint();
            _inputActions.Debug.PreviousCheckpoint.performed -= ctx => CheckpointsManager.Instance.TeleportToPreviousCheckpoint();

            DisableAllInputActions();
            DisableDebugInputActions();
        }

        public void EnablePlayerInputActions()
        {
            if (_inputActions == null)
                return;

            DisableAllInputActions();
            _inputActions.Player.Enable();
        }

        public void EnableUiInputActions()
        {
            if (_inputActions == null)
                return;

            DisableAllInputActions();
            _inputActions.Ui.Enable();
        }

        public void EnableDebugInputActions()
        {
            if (_inputActions == null)
                return;

            _inputActions.Debug.Enable();
        }

        public void DisableDebugInputActions()
        {
            if (_inputActions == null)
                return;

            _inputActions.Debug.Disable();
        }

        public void DisablePlayerInputActions()
        {
            if (_inputActions == null)
                return;

            _inputActions.Player.Disable();
        }

        private void DisableAllInputActions()
        {
            if (_inputActions == null)
                return;

            _inputActions.Player.Disable();
            _inputActions.Ui.Disable();
        }

        private void OnMoveStarted(InputAction.CallbackContext ctx) => _isMovementPressed = true;
        private void OnMoveCanceled(InputAction.CallbackContext ctx) => _isMovementPressed = false;

        private void OnCrouchStarted(InputAction.CallbackContext ctx) => CrouchPressed?.Invoke();
        private void OnCrouchCanceled(InputAction.CallbackContext ctx) => CrouchCanceled?.Invoke();

        private void OnInteractStarted(InputAction.CallbackContext ctx)
        {
            _isInteractPressed = true;
            InteractPressed?.Invoke();
        }

        private void OnInteractCanceled(InputAction.CallbackContext ctx)
        {
            _isInteractPressed = false;
            InteractCanceled?.Invoke();
        }

        private void OnThrowStarted(InputAction.CallbackContext ctx) => ThrowPressed?.Invoke();
        private void OnThrowCanceled(InputAction.CallbackContext ctx) => ThrowCanceled?.Invoke();

        private void OnDebugStarted(InputAction.CallbackContext ctx) => DebugPressed?.Invoke();

        private void OnJumpStarted(InputAction.CallbackContext ctx) => _isJumpPressed = true;
        private void OnJumpCanceled(InputAction.CallbackContext ctx) => _isJumpPressed = false;

        private void OnSprintStarted(InputAction.CallbackContext ctx) => _isSprintPressed = true;
        private void OnSprintCanceled(InputAction.CallbackContext ctx) => _isSprintPressed = false;

        private void OnPause(InputAction.CallbackContext ctx)
        {
            _isPausePressed = true;
            PausePressed?.Invoke();
            _isPausePressed = false;
        }

        private void OnUiEsc(InputAction.CallbackContext ctx) => UiEscPressed?.Invoke();
        private void OnUiEnter(InputAction.CallbackContext ctx) => UiEnterPressed?.Invoke();
        private void OnUiSpace(InputAction.CallbackContext ctx) => UiSpacePressed?.Invoke();
        private void OnUiTab(InputAction.CallbackContext ctx) => UiTabPressed?.Invoke();

        public Vector2 GetMovementInput() => _inputActions?.Player.Move.ReadValue<Vector2>() ?? Vector2.zero;
        public bool IsMoving() => GetMovementInput().magnitude > 0.01f;
        public Vector2 GetMousePosition() => _inputActions?.Player.Mouse.ReadValue<Vector2>() ?? Vector2.zero;

        public bool IsMovementPressed => _isMovementPressed;
        public bool IsJumpPressed { get => _isJumpPressed; set => _isJumpPressed = value; }
        public bool IsCrouchPressed => _isCrouchPressed;
        public bool IsSprintPressed => _isSprintPressed;

        public bool IsPausePressed => _isPausePressed;

        public bool IsInteractPressed => _isInteractPressed;
    }
}
