using System.Collections.Generic;
using _Scripts.Enemies.ViewTypes;
using _Scripts.Player;
using UnityEngine;

namespace _Scripts.Enemies.Skreecher.State
{
    public class SkreecherStateManager : MonoBehaviour, IEnemyStateManager<SkreecherStateManager>
    {
           public IEnemyState<SkreecherStateManager> PatrollingState { get; private set; }
        public IEnemyState<SkreecherStateManager> DetectingState { get; private set; }
        public IEnemyState<SkreecherStateManager> AggroState { get; private set; }
        public IEnemyState<SkreecherStateManager> InvestigatingState { get; private set; }
        public IEnemyState<SkreecherStateManager> DisabledState { get; private set; }
        public IEnemyState<SkreecherStateManager> CurrentState { get; private set; }
        public IEnemyState<SkreecherStateManager> PreviousState { get; private set; }
        
        [SerializeField] private SkreecherState enumState;

        [HideInInspector] public SkreecherSettings Settings;
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

        private void Awake()
        {
            Settings = GetComponent<SkreecherSettings>();
            Rigidbody2D = GetComponent<Rigidbody2D>();
            Collider2D = GetComponent<Collider2D>();
            ViewTypes = GetComponents<IViewType>();
            
            environmentLayer = LayerMask.GetMask("Environment");
            playerLayer = LayerMask.GetMask("Enemy");
            playerLayer = LayerMask.GetMask("Player");

            PatrollingState = new SkreecherPatrollingState();
            DetectingState = new SkreecherDetectingState();
            AggroState = new SkreecherAggroState();
            InvestigatingState = new SkreecherInvestigatingState();
            DisabledState = new SkreecherDisabledState();

            // Set the initial state
            CurrentState = PatrollingState;
            CurrentState.EnterState(this);
        }

        private void Update()
        {
            // Update the current state via its UpdateState function
            CurrentState.UpdateState();
            
            // TODO: Eventually the EnemyState ENUM can be removed entirely if we want
            // ENUM State is currently ONLY for debugging in inspector
            if (IsPatrollingState())
            {
                enumState = SkreecherState.Patrolling;
            }
            else if (IsDetectingState())
            {
                enumState = SkreecherState.Detecting;
            }
            else if (IsAggroState())
            {
                enumState = SkreecherState.Aggro;
            }
            else if (IsInvestigatingState())
            {
                enumState = SkreecherState.Investigating;
            }
            else if (IsDisabledState())
            {
                enumState = SkreecherState.Disabled;
            }
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            CurrentState.OnCollisionEnter2D(col);
        }

        private void OnCollisionStay2D(Collision2D col)
        {
            CurrentState.OnCollisionStay2D(col);
        }

        public void TransitionToState(IEnemyState<SkreecherStateManager> newState)
        {
            if (CurrentState == newState)
                return;
            
            // Disable state trap
            if (CurrentState == DisabledState)
            {
                Debug.Log("Tried to exit from DisabledState");
                return;
            }

            PreviousState = CurrentState;
            CurrentState.ExitState();
            CurrentState = newState;
            CurrentState.EnterState(this);
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

        public List<Collider2D> FindAllEnemiesInRange()
        {
            var enemies = new List<Collider2D>();
            foreach (var view in ViewTypes)
            {
                enemies.AddRange(view.GetAllEnemiesWithinView());
            }

            return enemies;
        }
        
        public void KillEnemy()
        {

            PlayerVariables.Instance.CommitSin(sinPenalty);
            TransitionToState(DisabledState);
        }
        
        public void AlertFromSkreecher() {/* Skreechers can't alert other skreechers */}

        #region State Getters
        public bool IsPatrollingState()
        {
            return CurrentState is SkreecherPatrollingState;
        }
        public bool IsDetectingState()
        {
            return CurrentState is SkreecherDetectingState;
        }
        public bool IsAggroState()
        {
            return CurrentState is SkreecherAggroState;
        }
        public bool IsDisabledState()
        {
            return CurrentState is SkreecherDisabledState;
        }
        
        // Alerted is not an actual state but is used to denote an increase in view radius / distance
        public bool IsAlertedState()
        {
            return CurrentState is SkreecherAggroState;
        }
        public bool IsInvestigatingState()
        {
            return CurrentState is SkreecherInvestigatingState;
        }
        
        public bool IsSearchingState()
        {
            return false;
        }
        public bool IsReturningState()
        {
            return false;
        }
        public bool IsStunnedState()
        {
            return false;
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