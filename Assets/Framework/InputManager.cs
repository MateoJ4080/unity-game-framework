using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using GameFramework.Events;

namespace GameFramework.Core
{
    /// <summary>
    /// Modern input events using Unity's New Input System
    /// </summary>
    [Serializable]
    public class ModernInputEvent
    {
        public string actionName;
        public InputActionPhase phase;
        public float timestamp;
        public Vector2 readValue;
        public InputDevice device;

        public ModernInputEvent(string name, InputActionPhase actionPhase, Vector2 value = default)
        {
            actionName = name;
            phase = actionPhase;
            timestamp = Time.time;
            readValue = value;
        }
    }

    /// <summary>
    /// Manages game input using Unity's New Input System
    /// </summary>
    public class InputManager : Singleton<InputManager>
    {
        [Header("Input Configuration")]
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private bool _enableInput = true;
        [SerializeField] private bool _debugMode = false;

        [Header("Action Map Settings")]
        [SerializeField] private string _gameplayActionMap = "Gameplay";
        [SerializeField] private string _uiActionMap = "UI";
        [SerializeField] private string _menuActionMap = "Menu";

        // Action Maps
        private InputActionMap _currentActionMap;
        private InputActionMap _gameplayMap;
        private InputActionMap _uiMap;
        private InputActionMap _menuMap;

        // Cached actions for performance and cleaner code
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _jumpAction;
        private InputAction _fireAction;
        private InputAction _interactAction;
        private InputAction _pauseAction;
        private InputAction _runAction;

        // Cached input values
        private Vector2 _movementInput;
        private Vector2 _lookInput;
        private bool _isRunning;

        // Events
        public System.Action<string, InputActionPhase, Vector2> OnInputAction;
        public System.Action<Vector2> OnMovementChanged;
        public System.Action<Vector2> OnLookChanged;

        // Properties for easy access
        public Vector2 MovementInput => _movementInput;
        public Vector2 LookInput => _lookInput;
        public bool IsRunning => _isRunning;
        public InputActionAsset InputActions => _inputActions;
        public bool InputEnabled => _enableInput;

        protected override void Awake()
        {
            base.Awake();

            if (_inputActions == null)
            {
                Debug.LogError("[InputManager] No Input Action Asset assigned! Please assign one in the inspector.");
                return;
            }

            InitializeInputSystem();
        }

        private void OnEnable()
        {
            EnableInput();
        }

        private void OnDisable()
        {
            DisableInput();
        }

        /// <summary>
        /// Initializes the input system and caches references
        /// </summary>
        private void InitializeInputSystem()
        {
            try
            {
                // Get Action Maps
                _gameplayMap = _inputActions.FindActionMap(_gameplayActionMap);
                _uiMap = _inputActions.FindActionMap(_uiActionMap);
                _menuMap = _inputActions.FindActionMap(_menuActionMap);

                if (_gameplayMap == null)
                {
                    Debug.LogError($"[InputManager] Action Map '{_gameplayActionMap}' not found!");
                    return;
                }
                if (_uiMap == null)
                {
                    Debug.LogError($"[InputManager] Action Map '{_uiActionMap}' not found!");
                }
                if (_menuMap == null)
                {
                    Debug.LogError($"[InputManager] Action Map '{_menuActionMap}' not found!");
                }

                // Cache main actions
                CacheInputActions();

                // Subscribe to callbacks
                SetupInputCallbacks();

                // Activate initial action map
                SwitchToActionMap(_gameplayActionMap);

                if (_debugMode)
                {
                    Debug.Log("[InputManager] Input System initialized successfully");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[InputManager] Error initializing input system: {e.Message}");
            }
        }

        /// <summary>
        /// Caches references to the most used actions
        /// </summary>
        private void CacheInputActions()
        {
            // Gameplay actions
            if (_gameplayMap != null)
            {
                _moveAction = _gameplayMap.FindAction("Move");
                _lookAction = _gameplayMap.FindAction("Look");
                _jumpAction = _gameplayMap.FindAction("Jump");
                _fireAction = _gameplayMap.FindAction("Fire");
                _interactAction = _gameplayMap.FindAction("Interact");
                _runAction = _gameplayMap.FindAction("Run");
            }

            // UI/Menu actions
            _pauseAction = _inputActions.FindAction("Pause") ??
                          _menuMap?.FindAction("Pause") ??
                          _uiMap?.FindAction("Pause");
        }

        /// <summary>
        /// Configures callbacks for input actions
        /// </summary>
        private void SetupInputCallbacks()
        {
            // Movement
            if (_moveAction != null)
            {
                _moveAction.performed += OnMovePerformed;
                _moveAction.canceled += OnMoveCanceled;
            }

            // Look
            if (_lookAction != null)
            {
                _lookAction.performed += OnLookPerformed;
                _lookAction.canceled += OnLookCanceled;
            }

            // Jump
            if (_jumpAction != null)
            {
                _jumpAction.started += ctx => HandleInputAction("Jump", InputActionPhase.Started);
                _jumpAction.performed += ctx => HandleInputAction("Jump", InputActionPhase.Performed);
                _jumpAction.canceled += ctx => HandleInputAction("Jump", InputActionPhase.Canceled);
            }

            // Fire
            if (_fireAction != null)
            {
                _fireAction.started += ctx => HandleInputAction("Fire", InputActionPhase.Started);
                _fireAction.performed += ctx => HandleInputAction("Fire", InputActionPhase.Performed);
                _fireAction.canceled += ctx => HandleInputAction("Fire", InputActionPhase.Canceled);
            }

            // Interact
            if (_interactAction != null)
            {
                _interactAction.started += ctx => HandleInputAction("Interact", InputActionPhase.Started);
                _interactAction.performed += ctx => HandleInputAction("Interact", InputActionPhase.Performed);
                _interactAction.canceled += ctx => HandleInputAction("Interact", InputActionPhase.Canceled);
            }

            // Run
            if (_runAction != null)
            {
                _runAction.started += ctx => _isRunning = true;
                _runAction.canceled += ctx => _isRunning = false;
            }

            // Pause
            if (_pauseAction != null)
            {
                _pauseAction.performed += ctx => HandleInputAction("Pause", InputActionPhase.Performed);
            }
        }

        /// <summary>
        /// Handles movement input
        /// </summary>
        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            _movementInput = context.ReadValue<Vector2>();
            OnMovementChanged?.Invoke(_movementInput);

            if (_debugMode && _movementInput != Vector2.zero)
            {
                Debug.Log($"[InputManager] Movement: {_movementInput}");
            }
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            _movementInput = Vector2.zero;
            OnMovementChanged?.Invoke(_movementInput);
        }

        /// <summary>
        /// Handles camera/look input
        /// </summary>
        private void OnLookPerformed(InputAction.CallbackContext context)
        {
            _lookInput = context.ReadValue<Vector2>();
            OnLookChanged?.Invoke(_lookInput);
        }

        private void OnLookCanceled(InputAction.CallbackContext context)
        {
            _lookInput = Vector2.zero;
            OnLookChanged?.Invoke(_lookInput);
        }

        /// <summary>
        /// Handles generic input actions
        /// </summary>
        private void HandleInputAction(string actionName, InputActionPhase phase)
        {
            if (!_enableInput) return;

            OnInputAction?.Invoke(actionName, phase, Vector2.zero);
            EventBus.Publish(new ModernInputEvent(actionName, phase));

            if (_debugMode)
            {
                Debug.Log($"[InputManager] Action '{actionName}' - Phase: {phase}");
            }
        }

        // Public API

        /// <summary>
        /// Switches to the specified action map
        /// </summary>
        public void SwitchToActionMap(string actionMapName)
        {
            if (_currentActionMap != null)
            {
                _currentActionMap.Disable();
            }

            InputActionMap targetMap = _inputActions.FindActionMap(actionMapName);
            if (targetMap != null)
            {
                _currentActionMap = targetMap;
                if (_enableInput)
                {
                    _currentActionMap.Enable();
                }

                if (_debugMode)
                {
                    Debug.Log($"[InputManager] Switched to action map: {actionMapName}");
                }
            }
            else
            {
                Debug.LogWarning($"[InputManager] Action map '{actionMapName}' not found!");
            }
        }

        /// <summary>
        /// Enables gameplay input mode
        /// </summary>
        public void EnableGameplayInput()
        {
            SwitchToActionMap(_gameplayActionMap);
        }

        /// <summary>
        /// Enables UI input mode
        /// </summary>
        public void EnableUIInput()
        {
            SwitchToActionMap(_uiActionMap);
        }

        /// <summary>
        /// Enables menu input mode
        /// </summary>
        public void EnableMenuInput()
        {
            SwitchToActionMap(_menuActionMap);
        }

        /// <summary>
        /// Globally enables or disables input
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            _enableInput = enabled;

            if (enabled)
            {
                EnableInput();
            }
            else
            {
                DisableInput();
            }

            if (_debugMode)
            {
                Debug.Log($"[InputManager] Input globally {(enabled ? "enabled" : "disabled")}");
            }
        }

        /// <summary>
        /// Enables the current input map
        /// </summary>
        public void EnableInput()
        {
            if (_currentActionMap != null && _enableInput)
            {
                _currentActionMap.Enable();
            }
        }

        /// <summary>
        /// Disables all input
        /// </summary>
        public void DisableInput()
        {
            _inputActions?.Disable();
        }

        /// <summary>
        /// Checks if an action is currently pressed
        /// </summary>
        public bool GetAction(string actionName)
        {
            if (!_enableInput) return false;

            InputAction action = _inputActions.FindAction(actionName);
            return action != null && action.IsPressed();
        }

        /// <summary>
        /// Checks if an action was triggered this frame
        /// </summary>
        public bool GetActionTriggered(string actionName)
        {
            if (!_enableInput) return false;

            InputAction action = _inputActions.FindAction(actionName);
            return action != null && action.WasPressedThisFrame();
        }

        /// <summary>
        /// Checks if an action was released this frame
        /// </summary>
        public bool GetActionReleased(string actionName)
        {
            if (!_enableInput) return false;

            InputAction action = _inputActions.FindAction(actionName);
            return action != null && action.WasReleasedThisFrame();
        }

        /// <summary>
        /// Reads the value of an action as a float
        /// </summary>
        public float ReadActionValue(string actionName)
        {
            if (!_enableInput) return 0f;

            InputAction action = _inputActions.FindAction(actionName);
            return action?.ReadValue<float>() ?? 0f;
        }

        /// <summary>
        /// Reads the value of an action as a Vector2
        /// </summary>
        public Vector2 ReadActionVector2(string actionName)
        {
            if (!_enableInput) return Vector2.zero;

            InputAction action = _inputActions.FindAction(actionName);
            return action?.ReadValue<Vector2>() ?? Vector2.zero;
        }

        /// <summary>
        /// Gets the device that triggered the last action
        /// </summary>
        public InputDevice GetLastUsedDevice()
        {
            return InputSystem.devices.Count > 0 ? InputSystem.devices[0] : null;
        }

        /// <summary>
        /// Checks if a gamepad is being used
        /// </summary>
        public bool IsUsingGamepad()
        {
            return Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame;
        }

        /// <summary>
        /// Checks if keyboard/mouse is being used
        /// </summary>
        public bool IsUsingKeyboardMouse()
        {
            return (Keyboard.current != null && Keyboard.current.wasUpdatedThisFrame) ||
                   (Mouse.current != null && Mouse.current.wasUpdatedThisFrame);
        }

        /// <summary>
        /// Gets all connected gamepads
        /// </summary>
        public List<Gamepad> GetConnectedGamepads()
        {
            List<Gamepad> gamepads = new List<Gamepad>();
            foreach (var device in InputSystem.devices)
            {
                if (device is Gamepad gamepad)
                {
                    gamepads.Add(gamepad);
                }
            }
            return gamepads;
        }

        /// <summary>
        /// Applies vibration to the gamepad (if connected)
        /// </summary>
        public void SetGamepadVibration(float lowFrequency, float highFrequency, float duration = 0.1f)
        {
            if (Gamepad.current != null)
            {
                Gamepad.current.SetMotorSpeeds(lowFrequency, highFrequency);

                // Stop vibration after the specified duration
                if (duration > 0)
                {
                    Invoke(nameof(StopGamepadVibration), duration);
                }
            }
        }

        /// <summary>
        /// Stops gamepad vibration
        /// </summary>
        public void StopGamepadVibration()
        {
            if (Gamepad.current != null)
            {
                Gamepad.current.SetMotorSpeeds(0, 0);
            }
        }

        /// <summary>
        /// Rebinds an action at runtime
        /// </summary>
        public void StartRebinding(string actionName, System.Action<string> onComplete = null)
        {
            InputAction action = _inputActions.FindAction(actionName);
            if (action == null)
            {
                Debug.LogWarning($"[InputManager] Action '{actionName}' not found for rebinding!");
                return;
            }

            action.Disable();

            var rebindOperation = action.PerformInteractiveRebinding()
                .WithControlsExcluding("Mouse")
                .OnMatchWaitForAnother(0.1f)
                .OnComplete(operation =>
                {
                    action.Enable();
                    operation.Dispose();
                    onComplete?.Invoke(actionName);

                    if (_debugMode)
                    {
                        Debug.Log($"[InputManager] Rebinding completed for '{actionName}'");
                    }
                });

            rebindOperation.Start();
        }

        /// <summary>
        /// Resets all bindings to their default values
        /// </summary>
        public void ResetAllBindings()
        {
            foreach (var map in _inputActions.actionMaps)
            {
                map.RemoveAllBindingOverrides();
            }

            if (_debugMode)
            {
                Debug.Log("[InputManager] All bindings reset to defaults");
            }
        }

        /// <summary>
        /// Returns current binding overrides as a JSON string
        /// </summary>
        public string SaveBindings()
        {
            return _inputActions.SaveBindingOverridesAsJson();
        }

        /// <summary>
        /// Loads bindings from JSON
        /// </summary>
        public void LoadBindings(string bindingOverrides)
        {
            if (!string.IsNullOrEmpty(bindingOverrides))
            {
                _inputActions.LoadBindingOverridesFromJson(bindingOverrides);

                if (_debugMode)
                {
                    Debug.Log("[InputManager] Custom bindings loaded");
                }
            }
        }

        // Cleanup - We use a Singleton pattern, so we need to ensure we clean up properly
        protected override void OnDestroy()
        {
            base.OnDestroy();
            DisableInput();
        }

        // // Convenience wrapper methods to mimic the old Unity Input API, allowing compatibility with legacy (old) systems
        public bool GetButtonDown(string actionName) => GetActionTriggered(actionName);
        public bool GetButton(string actionName) => GetAction(actionName);
        public bool GetButtonUp(string actionName) => GetActionReleased(actionName);
        public float GetAxis(string actionName) => ReadActionValue(actionName);
        public Vector2 GetMovementAxis() => MovementInput;
        public Vector2 GetLookAxis() => LookInput;
    }
}
