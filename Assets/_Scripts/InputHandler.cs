using System;
using _Scripts.Card;
using _Scripts.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts
{
    public class InputHandler : MonoBehaviour
    {
        #region Singleton

        public static InputHandler Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType(typeof(InputHandler)) as InputHandler;

                return _instance;
            }
            set { _instance = value; }
        }

        private static InputHandler _instance;

        #endregion

        private PlayerInputActions _inputActions;

        private void OnEnable()
        {
            if (_inputActions == null)
            {
                _inputActions = new PlayerInputActions();
            }

            _inputActions.Player.Enable();
            _inputActions.UI.Enable();

            // Subscribe to input events
            _inputActions.Player.Aim.performed += OnLookPerformed;
            _inputActions.Player.Aim.canceled += OnLookCanceled;

            _inputActions.Player.Throw.performed += OnThrowPerformed;
            _inputActions.Player.CancelCardThrow.performed += OnCancelCardThrow;
            _inputActions.Player.FalseTrigger.performed += OnFalseTriggerPerformed;

            _inputActions.UI.PauseEvent.performed += OnPausePerformed;
        }

        private void OnDisable()
        {
            _inputActions.Player.Aim.performed -= OnLookPerformed;
            _inputActions.Player.Aim.canceled -= OnLookCanceled;

            _inputActions.Player.Throw.performed -= OnThrowPerformed;
            _inputActions.Player.CancelCardThrow.performed -= OnCancelCardThrow;
            _inputActions.Player.FalseTrigger.performed -= OnFalseTriggerPerformed;
            
            _inputActions.UI.PauseEvent.performed -= OnPausePerformed;
            _inputActions.Player.Disable();
            _inputActions.UI.Disable();
        }

        // Event for updating direction while in card stance
        public event Action<Vector2> CardStanceDirectionalInput;

        // Event for handling card throw
        public event Action OnCardThrow;

        private Vector2 _lookInput;

        private void OnLookPerformed(InputAction.CallbackContext context)
        {
            if (PlayerVariables.Instance.stateManager.state == PlayerState.Stunned) return;
            if (CardManager.Instance.IsCardInScene()) return;

            _lookInput = context.ReadValue<Vector2>();

            if (_lookInput.magnitude > 0.1f)
            {
                Vector2 inputDirection = _lookInput.normalized;
                CardStanceDirectionalInput?.Invoke(inputDirection);
            }
            else
            {
                CardStanceDirectionalInput?.Invoke(Vector2.zero);
            }
        }

        private void OnLookCanceled(InputAction.CallbackContext context)
        {
            CardStanceDirectionalInput?.Invoke(Vector2.zero);
        }

        private void OnThrowPerformed(InputAction.CallbackContext context)
        {
            if (PlayerVariables.Instance.stateManager.state == PlayerState.Stunned) return;
            // Debug.Log("Throw Input");
            OnCardThrow?.Invoke();
        }
        
        public event Action OnFalseTrigger;
        private void OnFalseTriggerPerformed(InputAction.CallbackContext context)
        {
                /*
                 * The False trigger input is used to escape stuns. Even if it wasn't, it would be a clever way
                 * of escaping one regardless if the player already has an active card out and near the enemy.
                 * So, allow FalseTrigger input even if the player is stunned
                 */
                // Debug.Log("False Trigger Input");
                OnFalseTrigger?.Invoke();
        }

        public event Action OnCancelActiveCard;
        private void OnCancelCardThrow(InputAction.CallbackContext context)
        {
            // Debug.Log("Cancel throw");
            OnCancelActiveCard?.Invoke();
        }

        public event Action OnPausePressed;

        private void OnPausePerformed(InputAction.CallbackContext context)
        {
            Debug.Log("Pause Pressed");
            OnPausePressed?.Invoke();
        }
    }
}