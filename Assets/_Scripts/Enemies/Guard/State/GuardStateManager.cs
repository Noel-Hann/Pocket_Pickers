using System.Collections;
using _Scripts.Enemies.ViewTypes;
using _Scripts.Player;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace _Scripts.Enemies.Guard.State
{
    public class GuardStateManager : MonoBehaviour, IEnemyStateManager<GuardStateManager>
    {
        public IEnemyState<GuardStateManager> PatrollingState { get; private set; }
        public IEnemyState<GuardStateManager> DetectingState { get; private set; }
        public IEnemyState<GuardStateManager> AggroState { get; private set; }
        //public IEnemyState<GuardStateManager> AttackingState { get; private set; }
        public IEnemyState<GuardStateManager> SearchingState { get; private set; }
        public IEnemyState<GuardStateManager> ReturningState { get; private set; }
        public IEnemyState<GuardStateManager> InvestigatingState { get; private set; }
        public IEnemyState<GuardStateManager> StunnedState { get; private set; }
        public IEnemyState<GuardStateManager> DisabledState { get; private set; }
        public IEnemyState<GuardStateManager> CurrentState { get; private set; }
        public IEnemyState<GuardStateManager> PreviousState { get; private set; }
        
        [SerializeField] private GuardState enumState;

        [HideInInspector] public GuardSettings Settings;
        [HideInInspector] public Rigidbody2D Rigidbody2D;
        [HideInInspector] public Collider2D Collider2D;
        [HideInInspector] public IViewType[] ViewTypes;

        [Header("Gizmo Settings")]
        [SerializeField] private Color patrolPathColor = Color.green;
        [SerializeField] private float patrolPointRadius = 0.1f;

        [HideInInspector] public LayerMask environmentLayer;
        [HideInInspector] public LayerMask enemyLayer;
        [HideInInspector] public LayerMask playerLayer;
        
        [Header("Sin Values")]
        public int sinPenalty;

        public bool alertedFromAggroSkreecher;
        public bool alertedFromInvestigatingSkreecher;
        
        private ConeView coneView; 
        public Light2D visionConeLight;

        private void Awake()
        {
            Settings = GetComponent<GuardSettings>();
            Rigidbody2D = GetComponent<Rigidbody2D>();
            Collider2D = GetComponent<Collider2D>();
            ViewTypes = GetComponents<IViewType>();
            coneView = GetComponent<ConeView>(); // Access directly for the lighting effect
 
            environmentLayer = LayerMask.GetMask("Environment");
            playerLayer = LayerMask.GetMask("Enemy");
            playerLayer = LayerMask.GetMask("Player");

            PatrollingState = new GuardPatrollingState(this, transform.position ,Settings.leftPatrolDistance, Settings.rightPatrolDistance);
            DetectingState = new GuardDetectingState();
            AggroState = new GuardAggroState();
            // AttackingState = new GuardAttackingState();
            SearchingState = new GuardSearchingState();
            ReturningState = new GuardReturningState();
            InvestigatingState = new GuardInvestigatingState();
            StunnedState = new GuardStunnedState();
            DisabledState = new GuardDisabledState();

            // Set the initial state
            CurrentState = PatrollingState;
            CurrentState.EnterState(this);
        }

        private void Update()
        {
            // Handle ground detection and gravity in every state
            Settings.HandleGroundDetection();
            Settings.HandleGravity();

            // Update the current state via its UpdateState function
            CurrentState.UpdateState();

            // TODO: Eventually the EnemyState ENUM can be removed entirely if we want
            // ENUM State is currently ONLY for debugging in inspector
            if (IsPatrollingState())
            {
                enumState = GuardState.Patrolling;
            }
            else if (IsDetectingState())
            {
                enumState = GuardState.Detecting;
            }
            else if (IsAggroState())
            {
                enumState = GuardState.Aggro;
                //gameObject.GetComponent<Animator>().SetBool("Aggressive",true);
            }
            else if (IsSearchingState())
            {
                enumState = GuardState.Searching;
            }
            else if (IsReturningState())
            {
                enumState = GuardState.Returning;
            }
            else if (IsInvestigatingState())
            {
                enumState = GuardState.Investigating;
            }
            else if (IsStunnedState())
            {
                enumState = GuardState.Stunned;
            }
            else if (IsDisabledState())
            {
                enumState = GuardState.Disabled;
            }
            // else if (IsAttackingState())
            // {
            //     enumState = GuardState.Attacking;
            // }

            if (CurrentState == DisabledState || CurrentState == StunnedState)
            {
                visionConeLight.intensity = 0f;
                return;
            }
            
            UpdateVisionConeLight();
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            CurrentState.OnCollisionEnter2D(col);
        }

        private void OnCollisionStay2D(Collision2D col)
        {
            CurrentState.OnCollisionStay2D(col);
        }

        private void OnCollisionExit2D(Collision2D col)
        {
            CurrentState.OnCollisionExit2D(col);
        }

        public void TransitionToState(IEnemyState<GuardStateManager> newState)
        {
            if (CurrentState == newState)
                return;
            
            // Disable state trap
            if (CurrentState == DisabledState)
            {
                Debug.Log("Tried to exit from DisabledState");
                // return;
            }

            // Should immediately go to search state after being stunned, unless being disabled.
            if (CurrentState == StunnedState && (newState != SearchingState && newState != DisabledState))
            {
                Debug.Log("Tried to exit stunned to bad state");
                return;
            }

            PreviousState = CurrentState;
            CurrentState.ExitState();
            CurrentState = newState;
            CurrentState.EnterState(this);
        }
        private float _yPosLastFrame;

        public void Move(Vector2 direction, float speed)
        {
            if (Mathf.Abs(_yPosLastFrame) - Mathf.Abs(transform.position.y) >= 0.0030f &&
                !Settings.IsGrounded())
                StopMoving();
            else
                Rigidbody2D.velocity = new Vector2(direction.x * speed, Rigidbody2D.velocity.y);

            _yPosLastFrame = transform.position.y;

            // Rigidbody2D.velocity = Vector2.Lerp(transform.position, new Vector2(direction.x * speed, Rigidbody2D.velocity.y), Time.deltaTime);
        }

        public void StopMoving()
        {
            Rigidbody2D.velocity = new Vector2(0, Rigidbody2D.velocity.y);
        }

        public void StopFalling()
        {
            Rigidbody2D.velocity = new Vector2(Rigidbody2D.velocity.x, 0f); 
        }

        public void DashForward()
        {
            var dir = Settings.isFacingRight ? Vector2.right : Vector2.left;
            Rigidbody2D.velocity = dir * (PlayerVariables.Instance.Stats.DashSpeed * 0.1f);
        }

        public bool IsPlayerDetected()
        {
            foreach (var viewType in ViewTypes)
            {
                if (viewType.IsPlayerDetectedThisFrame())
                {
                    return true;
                }
            }
            return false;
        }
        
        public bool IsPlayerDetectedWithQuickDetect()
        {
            foreach (var viewType in ViewTypes)
            {
                if (viewType.IsPlayerDetectedThisFrame())
                {
                    if (viewType.QuickDetection())
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        
        public void AlertFromAggroSkreecher()
        {
            if (CurrentState == DisabledState) return;
            alertedFromAggroSkreecher = true;
            TransitionToState(AggroState);
        }

        public void AlertFromInvestigatingSkreecher()
        {
            if (CurrentState == DisabledState) return;
            alertedFromInvestigatingSkreecher = true;
            TransitionToState(InvestigatingState);
 
        }
        
        private void UpdateVisionConeLight()
        {
            var viewAngle = coneView.GetCurrentViewAngle();
            var viewDistance = coneView.GetCurrentViewDistance();
            
            // Debug.Log("View Angle: " + viewAngle);
            // Debug.Log("View Distance: " + viewDistance);

            visionConeLight.intensity = 0.85f;
            // visionConeLight.pointLightOuterAngle = viewAngle / 2f;
            visionConeLight.pointLightOuterAngle = viewAngle;
            visionConeLight.pointLightOuterRadius = viewDistance;
        }
        
        private void OnDrawGizmos()
        {
            if (PatrollingState != null)
            {
                var patrolState = PatrollingState as GuardPatrollingState;
                if (patrolState != null)
                {
                    // Get patrol points
                    Vector2 originPosition = patrolState.OriginPosition;
                    Vector2 leftPoint = patrolState.LeftPatrolPoint();
                    Vector2 rightPoint = patrolState.RightPatrolPoint();

                    // If not in play mode, use the transform position as origin
                    if (!Application.isPlaying)
                    {
                        originPosition = transform.position;
                        leftPoint = originPosition + Vector2.left * Settings.leftPatrolDistance;
                        rightPoint = originPosition + Vector2.right * Settings.rightPatrolDistance;
                    }

                    // Draw patrol path
                    Gizmos.color = patrolPathColor;
                    Gizmos.DrawLine(leftPoint, rightPoint);

                    // Draw patrol points
                    Gizmos.DrawSphere(leftPoint, patrolPointRadius);
                    Gizmos.DrawSphere(rightPoint, patrolPointRadius);
                }
            }
        }
        
        public void KillEnemy()
        {
            if (CurrentState == DisabledState) return;
            PlayerVariables.Instance.CommitSin(sinPenalty);
            TransitionToState(this.DisabledState);
        }
        
        public void KillEnemyWithoutGeneratingSin()
        {
            if (CurrentState == DisabledState) return;
            TransitionToState(DisabledState);
            StartCoroutine(DisableObjectWithDelay());
        }

        private IEnumerator DisableObjectWithDelay()
        {
            yield return new WaitForSeconds(0.75f);
            gameObject.SetActive(false);
        }

        #region State Getters
        public bool IsPatrollingState()
        {
            return CurrentState is GuardPatrollingState;
        }
        public bool IsDetectingState()
        {
            return CurrentState is GuardDetectingState;
        }
        public bool IsAggroState()
        {
            return CurrentState is GuardAggroState;
        }
        public bool IsSearchingState()
        {
            return CurrentState is GuardSearchingState;
        }
        public bool IsReturningState()
        {
            return CurrentState is GuardReturningState;
        }
        public bool IsStunnedState()
        {
            return CurrentState is GuardStunnedState;
        }
        public bool IsDisabledState()
        {
            return CurrentState is GuardDisabledState;
        }
        
        // Alerted is not an actual state but is used to denote an increase in view radius / distance
        public bool IsAlertedState()
        {
            return CurrentState is GuardAggroState || CurrentState is GuardSearchingState;
        }
        
        public bool IsInvestigatingState()
        {
            return CurrentState is GuardInvestigatingState;
        }
        
        // Sniper Specific
        public bool IsChargingState()
        {
            return false;
        }
        public bool IsReloadingState()
        {
            return false;
        }
        #endregion
    }
}