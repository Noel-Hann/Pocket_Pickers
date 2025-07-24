using UnityEngine;

namespace _Scripts.Enemies.Sniper.State
{
    public class SniperPatrollingState : IEnemyState<SniperStateManager>
    {
        private SniperStateManager _enemy;
        public void EnterState(SniperStateManager enemy)
        {
            Debug.Log("Enter Patrolling State");
            _enemy = enemy;
        }

        public void UpdateState()
        {
            if (_enemy.IsPlayerDetected())
            {
                Debug.Log("Player Detected in Patrol");
                _enemy.TransitionToState(_enemy.ChargingState); 
            }
        }

        public void ExitState() {}

        public void OnCollisionEnter2D(Collision2D col) {}

        public void OnCollisionStay2D(Collision2D col) {}
        public void OnCollisionExit2D(Collision2D col) { }
    }
}