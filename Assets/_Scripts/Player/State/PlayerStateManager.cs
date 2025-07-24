using System;
using System.Collections;
using _Scripts.Card;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.XR;

namespace _Scripts.Player.State
{
    public class PlayerStateManager : MonoBehaviour
    {
        // States
        public IPlayerState FreeMovingState { get; private set; }
        public IPlayerState DashingState { get; private set; }
        public IPlayerState WallState { get; private set; }
        public IPlayerState LedgeState { get; private set; }
        public IPlayerState StunnedState { get; private set; }
        public IPlayerState CurrentState { get; private set; }
        public IPlayerState PreviousState { get; private set; }

        [SerializeField] private PlayerState enumState;

        #region Singleton

        public static PlayerStateManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType(typeof(PlayerStateManager)) as PlayerStateManager;

                return _instance;
            }
            set { _instance = value; }
        }

        private static PlayerStateManager _instance;

        #endregion

        private void Awake()
        {
            // Initialize States
            FreeMovingState = new FreeMovingState();
            DashingState = new DashingState();
            WallState = new WallState();
            LedgeState = new LedgeState();
            StunnedState = new StunnedState();
        }

        private void Start()
        {
            // Set Initial State
            TransitionToState(FreeMovingState);
        }

        private void Update()
        {
            if (CurrentState == FreeMovingState)
                enumState = PlayerState.FreeMoving;
            if (CurrentState == StunnedState)
                enumState = PlayerState.Stunned;
            if (CurrentState == DashingState)
                enumState = PlayerState.Dashing;
            if (CurrentState == WallState)
                enumState = PlayerState.Wall;
            if (CurrentState == LedgeState)
                enumState = PlayerState.Ledge;
            
            CurrentState.UpdateState();
        }

        private void FixedUpdate()
        {
            CurrentState.FixedUpdateState();
        }

        public void TransitionToState(IPlayerState newState)
        {
            // State blocks
            if (CurrentState == newState) return;
            if (CurrentState == StunnedState && newState == DashingState) return;
            if (CurrentState == StunnedState && newState == WallState) return;
            
            // State blocks for movement being disabled
            if (newState == DashingState && !PlayerVariables.Instance.isDashEnabled) return;
            if (newState == WallState && !PlayerVariables.Instance.isWallClimbEnabled) return;

            Debug.Log("Exiting state");
            if (CurrentState != null)
                CurrentState.ExitState();

            PreviousState = CurrentState;
            CurrentState = newState;
            CurrentState.EnterState();
        }
        
        private void OnEnable()
        {
            if (InputHandler.Instance == null) return;
            InputHandler.Instance.OnDash += OnDashAction;
            PlayerMovement.Instance.Walled += HandleWallStateTransition;
            PlayerMovement.Instance.Ledged += HandleLedgeStateTransition;
        }
        private void OnDisable()
        {
            if (InputHandler.Instance == null) return;
            InputHandler.Instance.OnDash -= OnDashAction;
            PlayerMovement.Instance.Walled -= HandleWallStateTransition;
            PlayerMovement.Instance.Ledged -= HandleLedgeStateTransition;
        }

        #region Dash Transition
        private float _lastDashTime;
        private void OnDashAction()
        {
            // Cooldown check
            if (_lastDashTime + PlayerVariables.Instance.Stats.DashCooldown > PlayerVariables.Instance.Time)
            {
                Debug.Log("Dash cooldown");
                return;
            };
            
            _lastDashTime = PlayerVariables.Instance.Time;
            TransitionToState(DashingState);
        }
        #endregion

        private Coroutine _stunnedByPatroller;
        public void HandleHitByPatroller(bool struckFromRightSide)
        {
            if (_stunnedByPatroller != null)
                return;

            PlayerVariables.Instance.currentHealth--;

            if (PlayerVariables.Instance.currentHealth <= 0)
                GameManager.Instance.Die();

            Debug.Log("Player Hit by attack");
            //TransitionToState(StunnedState);
            _stunnedByPatroller = StartCoroutine(StunnedByPatrollerHitCountdown());
            PlayerMovement.Instance.ApplyDiagonalForce(!struckFromRightSide);
        }

        private IEnumerator StunnedByPatrollerHitCountdown()
        {
            yield return new WaitForSeconds(1.5f);
            TransitionToState(FreeMovingState);
            _stunnedByPatroller = null;
        }
        
        #region Wall Sliding

        public float lastWallHangTime;

        private void HandleWallStateTransition(bool isWalled)
        {
            if (CurrentState == WallState)
            {
                lastWallHangTime = Time.time;
                return;
            }
            
            // PlayerAnimator.Instance.endHang();

            if (!isWalled || PlayerMovement.Instance.IsLedged())
            {
                return;
            }

            if (PlayerMovement.Instance.JumpHeldFrameInput && PlayerMovement.Instance.CurrentFrameVelocity.y > 0f)
            {
                return;
            }

            if (lastWallHangTime + PlayerVariables.Instance.Stats.WallHangCooldown > Time.time)
            {
                Debug.Log("Wall hang cooldown");
                return;
            }

            // is not grounded, has downward velocity, and is moving the left stick in the direction of the wall they hit...
            if (!PlayerMovement.Instance.IsGrounded() &&
                ((PlayerVariables.Instance.isFacingRight && PlayerMovement.Instance.FrameInput.x > 0) ||
                 (!PlayerVariables.Instance.isFacingRight && PlayerMovement.Instance.FrameInput.x < 0)))
            {
                Debug.Log("Transitioning to Wall State");
                TransitionToState(WallState);
            }
        }
        
        public void SetLastWallHangTime(float time)
        {
            lastWallHangTime = time;
        }
        #endregion
        
        #region Ledge Hang

        //TODO set this up to make a minimum time between ending one ledge hang and starting another
        public float lastLedgeHangTime;
        private void HandleLedgeStateTransition(bool isLedged)
        {
            if (CurrentState == LedgeState) return;

            if (!PlayerMovement.Instance.IsLedged() || !isLedged) return;
            
            if (lastLedgeHangTime + PlayerVariables.Instance.Stats.LedgeHangCooldown > Time.time)
            {
                Debug.Log("Ledge hang cooldown");
                return;
            }
            
            if (PlayerMovement.Instance.JumpHeldFrameInput && PlayerMovement.Instance.CurrentFrameVelocity.y > 0f)
            {
                return;
            }
            
            // is touching a wall, is not grounded, and is moving the left stick in the direction of the wall they hit...
            if (!PlayerMovement.Instance.IsGrounded() &&
                PlayerMovement.Instance.IsWalled() &&
                ((PlayerVariables.Instance.isFacingRight && PlayerMovement.Instance.FrameInput.x > 0) ||
                 (!PlayerVariables.Instance.isFacingRight && PlayerMovement.Instance.FrameInput.x < 0)))
            {
                Debug.Log("Transitioning to Ledge State");
                TransitionToState(LedgeState);
            }
        }

        public void setLastLedgeHangTime(float time)
        {
            lastLedgeHangTime = time;
        }
        #endregion

        public bool IsStunnedState()
        {
            return CurrentState is StunnedState;
        }

        
        public bool IsWallState()
        {
            return CurrentState is WallState;
        }

        public bool IsLedgeState()
        {
            return CurrentState is LedgeState;
        }
    }
}