using TMPro;
using UnityEngine;

namespace _Scripts.Enemies.Skreecher.State
{
    public class SkreecherPatrollingState : IEnemyState<SkreecherStateManager>
    {
        private SkreecherStateManager _enemy;
        public void EnterState(SkreecherStateManager enemy)
        {
            _enemy = enemy;
            Debug.Log("Enter Patrolling");
        }

        public void UpdateState()
        {
            if (_enemy.IsPlayerDetectedWithQuickDetect())
                _enemy.TransitionToState(_enemy.AggroState);
            if (_enemy.IsPlayerDetected())
            {
                Debug.Log("Hit");
                _enemy.TransitionToState(_enemy.DetectingState);
            }
        }

        public void ExitState()
        {
        }

        public void OnCollisionEnter2D(Collision2D col)
        {
        }

        public void OnCollisionStay2D(Collision2D col)
        {
        }
        public void OnCollisionExit2D(Collision2D col) { }
    }
}