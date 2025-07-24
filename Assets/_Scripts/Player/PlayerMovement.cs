using System;
using System.Collections;
using System.Collections.Generic;
using _Scripts.Card;
using _Scripts.Player.State;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts.Player
{
    /// <summary>
    /// Hey!
    /// Tarodev here. I built this controller as there was a severe lack of quality & free 2D controllers out there.
    /// I have a premium version on Patreon, which has every feature you'd expect from a polished controller. Link: https://www.patreon.com/tarodev
    /// You can play and compete for best times here: https://tarodev.itch.io/extended-ultimate-2d-controller
    /// If you hve any questions or would like to brag about your score, come to discord: https://discord.gg/tarodev
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerMovement : MonoBehaviour, IPlayerController
    {
        private FrameInput _frameInput;
        private Vector2 _frameVelocity;
        private bool _cachedQueryStartInColliders;
        private bool _reduceInputsWhileReadingWallJumpApex;

        public LayerMask wallLayer;
        

        #region Interface

        public Vector2 FrameInput => _frameInput.Move;
        public bool JumpDownFrameInput => _frameInput.JumpDown;
        public bool JumpHeldFrameInput => _frameInput.JumpHeld;
        public Vector2 CurrentFrameVelocity => _frameVelocity;
        public event Action<bool, float> GroundedChanged;
        public event Action Jumped;

        #endregion
        #region Singleton

        public static PlayerMovement Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType(typeof(PlayerMovement)) as PlayerMovement;

                return _instance;
            }
            set { _instance = value; }
        }

        private static PlayerMovement _instance;

        #endregion

        private float _time;

        private void Awake()
        {
            _cachedQueryStartInColliders = Physics2D.queriesStartInColliders;
        }

        private void OnEnable()
        {
            CardManager.Instance.Teleport += TeleportTo;
            InputHandler.Instance.OnCrouch += ToggleCrouching;
            InputHandler.Instance.OnJumpPressed += HandleJumpPressed;

        }

        private bool _jumpPressedThisFrame;
        private void HandleJumpPressed()
        {
            _jumpPressedThisFrame = true;
        }

        private void Update()
        {
            _time += Time.deltaTime;

            CheckWallStatus();
            CheckLedgeStatus(); 
            

            // Allow full player control again after reaching the apex of a wall jump
            if (_reduceInputsWhileReadingWallJumpApex && _frameVelocity.y <= 0)
                _reduceInputsWhileReadingWallJumpApex = false;
        }

        private bool _movementBlockedLastFrame; 
        public void GatherInput()
        {
            // Do not allow player movement when paused or dead
            if (GameManager.Instance is not null)
                if (GameManager.Instance.isDead || PauseMenu.IsPaused)
                {
                    _frameInput = new FrameInput();
                    _movementBlockedLastFrame = true;
                    return;
                }

            if (_movementBlockedLastFrame)
            {
                _frameInput = new FrameInput();
                _movementBlockedLastFrame = !_movementBlockedLastFrame;
                return;
            }
            
            if (_reduceInputsWhileReadingWallJumpApex)
            {
                _frameInput = new FrameInput
                {
                    JumpHeld = InputHandler.Instance.JumpHeld,
                    // Reduce the amount of control the player has while in a wall jump to force horizontal movement
                    // in the opposite direction without feeling clunky
                    Move = InputHandler.Instance.MovementInput * new Vector2(0.25f, 0.25f)
                };
            }
            else
            {
                // Normal input tracking
                _frameInput = new FrameInput
                {
                    JumpDown = _jumpPressedThisFrame,
                    JumpHeld = InputHandler.Instance.JumpHeld,
                    Move = InputHandler.Instance.MovementInput
                }; 
            }
            
            // Track the facing direction based on the last non-zero horizontal input
            if (_frameInput.Move.x != 0)
            {
                // PlayerVariables.Instance.isFacingRight = _frameInput.Move.x > 0;
                if ((PlayerVariables.Instance.isFacingRight && _frameInput.Move.x < 0)||
                    (!PlayerVariables.Instance.isFacingRight && _frameInput.Move.x > 0))
                {
                    PlayerVariables.Instance.FlipLocalScale();
                }
                
            }
        
            if (PlayerVariables.Instance.Stats.SnapInput)
            {
                _frameInput.Move.x = Mathf.Abs(_frameInput.Move.x) < PlayerVariables.Instance.Stats.HorizontalDeadZoneThreshold ? 0 : Mathf.Sign(_frameInput.Move.x);
                _frameInput.Move.y = Mathf.Abs(_frameInput.Move.y) < PlayerVariables.Instance.Stats.VerticalDeadZoneThreshold ? 0 : Mathf.Sign(_frameInput.Move.y);
            }
        
            if (_frameInput.JumpDown)
            {
                _jumpToConsume = true;
                _timeJumpWasPressed = _time;
            }

            _jumpPressedThisFrame = false;
        }

        public void TeleportTo(Vector2 location)
        {
            
            // Handle effect
            
            // Handle Teleportation
            gameObject.transform.position = location;
            gameObject.transform.rotation = Quaternion.identity;
            //gameObject.GetComponent<Rigidbody2D>().AddForceAtPosition(new Vector2(0,1),);
            
            _frameVelocity.y = 15f;
            _frameVelocity.x = 0f;
            StartCoroutine(TeleportHangTime());
        }

        private bool _isHangingAfterTp;
        private IEnumerator TeleportHangTime()
        {
            _isHangingAfterTp = true;
            yield return new WaitForSeconds(PlayerVariables.Instance.Stats.TeleportHangTime);
            _isHangingAfterTp = false;
        }

        #region Collisions
        
        private float _frameLeftGrounded = float.MinValue;
        private bool _grounded;

        public bool IsGrounded() { return _grounded;}
        public void CheckCollisions()
        {
            Physics2D.queriesStartInColliders = false;

            // Ground and Ceiling
            var origin = (Vector2)PlayerVariables.Instance.Collider2D.transform.position + PlayerVariables.Instance.Collider2D.offset;

            bool groundHit = Physics2D.BoxCast(
                origin,
                PlayerVariables.Instance.Collider2D.size,
                0f,
                Vector2.down,
                PlayerVariables.Instance.Stats.GrounderDistance,
                ~PlayerVariables.Instance.Stats.PlayerLayer
            );

            bool ceilingHit = Physics2D.BoxCast(
                origin,
                PlayerVariables.Instance.Collider2D.size,
                0f,
                Vector2.up,
                PlayerVariables.Instance.Stats.GrounderDistance,
                ~PlayerVariables.Instance.Stats.PlayerLayer
            );

            // Hit a Ceiling
            if (ceilingHit) _frameVelocity.y = Mathf.Min(0, _frameVelocity.y);

            // Landed on the Ground
            if (!_grounded && groundHit)
            {
                _grounded = true;
                _coyoteUsable = true;
                _bufferedJumpUsable = true;
                _endedJumpEarly = false;
                GroundedChanged?.Invoke(true, Mathf.Abs(_frameVelocity.y));
            }
            // Left the Ground
            else if (_grounded && !groundHit)
            {
                _grounded = false;
                _frameLeftGrounded = _time;
                GroundedChanged?.Invoke(false, 0);
            }

            Physics2D.queriesStartInColliders = _cachedQueryStartInColliders;
        }

        #endregion


        #region Jumping

        private bool _jumpToConsume;
        private bool _bufferedJumpUsable;
        private bool _endedJumpEarly;
        private bool _coyoteUsable;
        private float _timeJumpWasPressed;

        public bool HasBufferedJump => _bufferedJumpUsable && _time < _timeJumpWasPressed + PlayerVariables.Instance.Stats.JumpBuffer;
        public bool CanUseCoyote => _coyoteUsable && !_grounded && _time < _frameLeftGrounded + PlayerVariables.Instance.Stats.CoyoteTime;

        public void HandleJump()
        {
            
            if (!_endedJumpEarly && !_grounded && !_frameInput.JumpHeld && PlayerVariables.Instance.RigidBody2D.velocity.y > 0) _endedJumpEarly = true;
            
            if (!_jumpToConsume && !HasBufferedJump) return;

            if (_grounded || CanUseCoyote || CanUseWallCoyote)
            {
                /*Debug.Log("======================");
                Debug.Log("Jump Report:");
                Debug.Log("Grounded: " + _grounded);
                Debug.Log("Coyote: " + CanUseCoyote);
                Debug.Log("Wall Coyote: " + CanUseWallCoyote);
                Debug.Log("Walled: " + _isWalled);
                Debug.Log("Last wall hang: " + PlayerStateManager.Instance.lastWallHangTime);
                Debug.Log("======================");*/
                ExecuteJump();
            }
            

            _jumpToConsume = false;
        }

        public void ExecuteJump()
        {
            _endedJumpEarly = false;
            _timeJumpWasPressed = 0;
            _bufferedJumpUsable = false;
            _coyoteUsable = false;
            _wallCoyoteUsable = false;
            if (!_isCrouching)
            {
                _frameVelocity.y = PlayerVariables.Instance.Stats.JumpPower;
            }
            else
            {
                _frameVelocity.y = PlayerVariables.Instance.Stats.CrouchJumpPower; 
            }
            Jumped?.Invoke();
        }


        public void ExecuteWallJump()
        {
            _endedJumpEarly = false;
            _timeJumpWasPressed = 0;
            _bufferedJumpUsable = false;
            _coyoteUsable = false;
            _wallCoyoteUsable = false;
            _frameVelocity.y = PlayerVariables.Instance.Stats.WallJumpPower;
            
            // TODO: Adjust this?
            _frameVelocity.x = PlayerVariables.Instance.isFacingRight ? -_frameVelocity.y : _frameVelocity.y; 
            PlayerVariables.Instance.FlipLocalScale();
            _reduceInputsWhileReadingWallJumpApex = true;
            
            Jumped?.Invoke();
        }

        #endregion

        #region Horizontal

        public void AlterHorizontalMovement(float amount)
        {
            _frameVelocity.x *= amount;
        }

        private bool _isCrouching;

        private void ToggleCrouching() => _isCrouching = !_isCrouching;

        public void HandleDirection()
        {
            if (_frameInput.Move.x == 0)
            {
                var deceleration = _grounded ? PlayerVariables.Instance.Stats.GroundDeceleration : PlayerVariables.Instance.Stats.AirDeceleration;
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, 0, deceleration * Time.fixedDeltaTime);
            } 
            else if (_isCrouching)
            {
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, _frameInput.Move.x * PlayerVariables.Instance.Stats.MaxCrouchSpeed, PlayerVariables.Instance.Stats.Acceleration * Time.fixedDeltaTime);
            }
            else
            {
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, _frameInput.Move.x * PlayerVariables.Instance.Stats.MaxSpeed, PlayerVariables.Instance.Stats.Acceleration * Time.fixedDeltaTime);
            }
        }

        #endregion

        #region Gravity

        public void HandleGravity()
        {
            if (_grounded && _frameVelocity.y <= 0f)
            {
                _frameVelocity.y = PlayerVariables.Instance.Stats.GroundingForce;
            }
            else if (_isHangingAfterTp)
            {
                _frameVelocity.y = Mathf.Lerp(_frameVelocity.y, -PlayerVariables.Instance.Stats.MaxFallSpeed, Time.deltaTime * 2f);
            }
            else
            {
                var inAirGravity = PlayerVariables.Instance.Stats.FallAcceleration;
                if (_endedJumpEarly && _frameVelocity.y > 0) inAirGravity *= PlayerVariables.Instance.Stats.JumpEndEarlyGravityModifier;
                _frameVelocity.y = Mathf.MoveTowards(_frameVelocity.y, -PlayerVariables.Instance.Stats.MaxFallSpeed, inAirGravity * Time.fixedDeltaTime);
            }
        }

        #endregion


        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("EscapeRout"))
            {
               // PlayerVariables.Instance.Escape();
               GameManager.Instance.EscapeLevel();
            }
        }

       
        public void ApplyMovement() => PlayerVariables.Instance.RigidBody2D.velocity = _frameVelocity;

        public void HaltHorizontalMomentum()
        {
            // _frameInput.Move.x = 0f;
            _frameVelocity.x = 0f;
        }
        
        public void HaltVerticalMomentum()
        {
            _frameVelocity.y = 0f;
        }

        public void LerpVerticalMomentum()
        {
            _frameInput.Move.y = 0f;
            _frameVelocity.y = Mathf.Lerp(_frameVelocity.y, 0f, Time.deltaTime * 750f);
        }
        #region Dashing
        public Vector2 DashDirection { get; set; } 
        public void ApplyDashMovement()
        {
            _frameVelocity = DashDirection * PlayerVariables.Instance.Stats.DashSpeed;
            ApplyMovement();
        }
        #endregion
        
        #region WallSliding

        public Transform wallCheck;
        public Action<bool> Walled;
        private bool _isWalled;
        // private float _frameLeftWalledWhileWalledState;
        private bool _wallCoyoteUsable;
        public bool CanUseWallCoyote => _wallCoyoteUsable && !IsWalled() && _time < PlayerStateManager.Instance.lastWallHangTime + PlayerVariables.Instance.Stats.WallCoyoteTime;

        private void CheckWallStatus()
        {
            var wasWalled = _isWalled;
            _isWalled = IsWalled();

            // if (wasWalled && !_isWalled)
            // {
            //     _frameLeftWalled = Time.time;
            // }

            if (!_isWalled) return;
            if (!_reduceInputsWhileReadingWallJumpApex)
                _wallCoyoteUsable = true;
            Walled?.Invoke(_isWalled);
        }

        public bool IsWalled()
        {
            return Physics2D.OverlapCircle(wallCheck.position, 0.2f, wallLayer);
        }
        
        /*
         * Handles the downward slide when on a wall as well as preventing any horizontal player movement
         */
        public void WallSlideMovement()
        {
            _frameVelocity.y = Mathf.MoveTowards(_frameVelocity.y, -PlayerVariables.Instance.Stats.WallSlideSpeed, Time.fixedDeltaTime);
            // _frameVelocity.x = 0f;
        }
        
        
        public void HandleWallJump()
        {
            if (!_endedJumpEarly && !_grounded && !_frameInput.JumpHeld && PlayerVariables.Instance.RigidBody2D.velocity.y > 0) _endedJumpEarly = true;

            if (!_jumpToConsume && !HasBufferedJump) return;

            if (IsWalled() || CanUseWallCoyote) ExecuteWallJump();

            _jumpToConsume = false;
        }

        /*
        * Since Movement gets halted during transitions to wall state and ledge state sometimes the animations have
        * a small gap between the player and wall. Pushing the player towards a wall while in wall or ledge state
        * Makes sure that the sprites are always touching
        */
        public void PushPlayerTowardsWall()
        {
            float direction;
            if (PlayerStateManager.Instance.IsLedgeState() || PlayerStateManager.Instance.IsWallState())
                direction = PlayerVariables.Instance.isFacingRight ? 1f : -1f;
            else return;
            
            _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, direction * PlayerVariables.Instance.Stats.MaxSpeed, PlayerVariables.Instance.Stats.Acceleration * Time.fixedDeltaTime); 
        }

        #endregion
        
        #region Ledge Hanging
        private bool _onLedge;
        public event Action<bool> Ledged; 
        public void CheckLedgeStatus()
        {
            if (PlayerStateManager.Instance.IsLedgeState())
                return;

            var hit = PerformLedgeCheckCast();

            // If a hit was detected the player is NOT on a ledge
            if (hit.collider != null)
            {
                Ledged?.Invoke(false);
                
                return;
            }
            
            if (!PlayerMovement.Instance.IsGrounded() && IsWalled() &&
                ((PlayerVariables.Instance.isFacingRight && PlayerMovement.Instance.FrameInput.x > 0) ||
                 (!PlayerVariables.Instance.isFacingRight && PlayerMovement.Instance.FrameInput.x < 0)))
            {
                //TODO this cannot be called if you just got off a ledge
                Ledged?.Invoke(true);
            }
        }

        public bool IsLedged()
        {
            var hit = PerformLedgeCheckCast();

            if (IsWalled() && hit.collider == null)
                return true;
            return false;
        }

        private RaycastHit2D PerformLedgeCheckCast()
        {
            var rayOrigin = (Vector2)PlayerVariables.Instance.Collider2D.bounds.center + Vector2.up * (PlayerVariables.Instance.Collider2D.bounds.extents.y - 0.4f);
            var direction = PlayerVariables.Instance.isFacingRight ? Vector2.right : Vector2.left;
            var hit = Physics2D.Raycast(rayOrigin, direction, 0.7f, PlayerMovement.Instance.wallLayer);
            return hit;
        }
        #endregion

        public void ApplyDiagonalForce(bool facingRight)
        {
            var verticalPower = PlayerVariables.Instance.Stats.JumpPower * 0.8f;
            var horizontalPower = PlayerVariables.Instance.Stats.MaxSpeed * 1.85f;

            var diagonalForce = new Vector2(
                facingRight ? horizontalPower : -horizontalPower,
                verticalPower
            );

            _frameVelocity = diagonalForce;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (PlayerVariables.Instance != null && PlayerVariables.Instance.Stats == null) Debug.LogWarning("Please assign a ScriptableStats asset to the Player Controller's Stats slot", this);
        }
#endif
    }

    public struct FrameInput
    {
        public bool JumpDown;
        public bool JumpHeld;
        public Vector2 Move;
    }

    public interface IPlayerController
    {
        public event Action<bool, float> GroundedChanged;

        public event Action Jumped;
        public Vector2 FrameInput { get; }
    }
}