using System;
using _Scripts.Card;
using _Scripts.Enemies;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

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
    public class PlayerMovementController : MonoBehaviour, IPlayerController
    {
        
        /*
         *The plan:
         * Subscribe to the event listener for Teleport, belonging to the Card class.
         * When the event is called, call a function that accepts the vector2
         * Set the player's transform to be equal to the vector2 passed in
         * Set the players speed to 0
         */
        [SerializeField] private ScriptableStats _stats;
        private Rigidbody2D _rb;
        private BoxCollider2D _col;
        private PlayerStateManager _stateManager;
        private FrameInput _frameInput;
        private Vector2 _frameVelocity;
        private bool _cachedQueryStartInColliders;

        #region Interface

        public Vector2 FrameInput => _frameInput.Move;
        public event Action<bool, float> GroundedChanged;
        public event Action Jumped;

        #endregion
        #region Singleton

        public static PlayerMovementController Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType(typeof(PlayerMovementController)) as PlayerMovementController;

                return _instance;
            }
            set { _instance = value; }
        }

        private static PlayerMovementController _instance;

        #endregion

        private float _time;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<BoxCollider2D>();
            _stateManager = GetComponent<PlayerStateManager>();

            _cachedQueryStartInColliders = Physics2D.queriesStartInColliders;
        }

        private void OnEnable()
        {
            
            //TODO make it subscribe to Card's Teleport event
            CardManager.Instance.Teleport += teleportTo;
        }
        private void Update()
        {
            _time += Time.deltaTime;
            if (_stateManager.state != PlayerState.Stunned)
            {
                GatherInput();
            }
        }
        
        /**
         * When the player is holding the card stance button they should not be allowed
         * to make other movement inputs
         */

        public void EnterCardStance()
        {
           // _stateManager.SetState(PlayerState.CardStance);
        }

        public void ExitCardStance()
        {
            _stateManager.SetState(PlayerState.Idle);
        }

        
        private void GatherInput()
        {
            if (_stateManager.state == PlayerState.Stunned) return; // Safety check
            
            _frameInput = new FrameInput
            {
                JumpDown = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.C),
                JumpHeld = Input.GetButton("Jump") || Input.GetKey(KeyCode.C),
                Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"))
            };
            
            // Track the facing direction based on the last non-zero horizontal input
            if (_frameInput.Move.x != 0)
            {
                // PlayerVariables.Instance.isFacingRight = _frameInput.Move.x > 0;
                if ((PlayerVariables.Instance.isFacingRight && _frameInput.Move.x < 0)||
                    (!PlayerVariables.Instance.isFacingRight && _frameInput.Move.x > 0))
                {
                    PlayerVariables.Instance.FlipLocalScale();
                }
                
                    _stateManager.SetState(PlayerState.Moving);
            }
            else
            {
                    _stateManager.SetState(PlayerState.Idle);
            }

            if (_stats.SnapInput)
            {
                _frameInput.Move.x = Mathf.Abs(_frameInput.Move.x) < _stats.HorizontalDeadZoneThreshold ? 0 : Mathf.Sign(_frameInput.Move.x);
                _frameInput.Move.y = Mathf.Abs(_frameInput.Move.y) < _stats.VerticalDeadZoneThreshold ? 0 : Mathf.Sign(_frameInput.Move.y);
            }

            if (_frameInput.JumpDown)
            {
                _jumpToConsume = true;
                _timeJumpWasPressed = _time;
            }
        }

        private void FixedUpdate()
        {
            CheckCollisions();

            HandleJump();
            HandleDirection();
            HandleGravity();
            
            ApplyMovement();
        }

        private void teleportTo(Vector2 location)
        {
            gameObject.transform.position = location;
            gameObject.transform.rotation = Quaternion.identity;
            //todo set the player's velocity to 0
            
        }

        #region Collisions
        
        private float _frameLeftGrounded = float.MinValue;
        private bool _grounded;

        public bool isGrounded() { return _grounded;}
        private void CheckCollisions()
        {
            Physics2D.queriesStartInColliders = false;

            // Ground and Ceiling
            var origin = (Vector2)_col.transform.position + _col.offset;

            bool groundHit = Physics2D.BoxCast(
                origin,
                _col.size,
                0f,
                Vector2.down,
                _stats.GrounderDistance,
                ~_stats.PlayerLayer
            );

            bool ceilingHit = Physics2D.BoxCast(
                origin,
                _col.size,
                0f,
                Vector2.up,
                _stats.GrounderDistance,
                ~_stats.PlayerLayer
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

        private bool HasBufferedJump => _bufferedJumpUsable && _time < _timeJumpWasPressed + _stats.JumpBuffer;
        private bool CanUseCoyote => _coyoteUsable && !_grounded && _time < _frameLeftGrounded + _stats.CoyoteTime;

        private void HandleJump()
        {
            if (!_endedJumpEarly && !_grounded && !_frameInput.JumpHeld && _rb.velocity.y > 0) _endedJumpEarly = true;

            if (!_jumpToConsume && !HasBufferedJump) return;

            if (_grounded || CanUseCoyote) ExecuteJump();

            _jumpToConsume = false;
        }

        private void ExecuteJump()
        {
            _endedJumpEarly = false;
            _timeJumpWasPressed = 0;
            _bufferedJumpUsable = false;
            _coyoteUsable = false;
            _frameVelocity.y = _stats.JumpPower;
            Jumped?.Invoke();
        }

        #endregion

        #region Horizontal

        private void HandleDirection()
        {
            if (_frameInput.Move.x == 0)
            {
                var deceleration = _grounded ? _stats.GroundDeceleration : _stats.AirDeceleration;
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, 0, deceleration * Time.fixedDeltaTime);
            } 
            else if (_stateManager.state == PlayerState.Stunned)
            {
                _frameVelocity.x = 0f;
            }
            else
            {
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, _frameInput.Move.x * _stats.MaxSpeed, _stats.Acceleration * Time.fixedDeltaTime);
            }
        }

        #endregion

        #region Gravity

        private void HandleGravity()
        {
            if (_grounded && _frameVelocity.y <= 0f)
            {
                _frameVelocity.y = _stats.GroundingForce;
            }
            else
            {
                var inAirGravity = _stats.FallAcceleration;
                if (_endedJumpEarly && _frameVelocity.y > 0) inAirGravity *= _stats.JumpEndEarlyGravityModifier;
                _frameVelocity.y = Mathf.MoveTowards(_frameVelocity.y, -_stats.MaxFallSpeed, inAirGravity * Time.fixedDeltaTime);
            }
        }

        #endregion


        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("EscapeRout"))
            {
                PlayerVariables.Instance.escape();
            }
        }

       
        private void ApplyMovement() => _rb.velocity = _frameVelocity;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_stats == null) Debug.LogWarning("Please assign a ScriptableStats asset to the Player Controller's Stats slot", this);
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