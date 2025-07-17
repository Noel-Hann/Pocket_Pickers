using System.Collections;
using UnityEngine;

namespace _Scripts.Enemies.Guard.State
{
    public class GuardDetectingState : IEnemyState<GuardStateManager>
    {
        private float _detectionTimer;
        private GuardStateManager _enemy;
        private bool _isWaiting;
        private Coroutine _waitCoroutine;

        public void EnterState(GuardStateManager enemy)
        {
            _enemy = enemy;
            _detectionTimer = 0f;
            _enemy.StopMoving();
        }

        public void UpdateState()
        {
            if (!_enemy.IsPlayerDetected())
            {
                _isWaiting = true;
                _waitCoroutine = _enemy.StartCoroutine(WaitBeforeSwitchingBackToPatrol());
                return;
            }

            _detectionTimer += Time.deltaTime * _enemy.Settings.DetectionModifier; 
            
            if (_enemy.IsPlayerDetectedWithQuickDetect())
            {
                if (_detectionTimer < _enemy.Settings.quickDetectionTime)
                    return;
                
                _enemy.TransitionToState(_enemy.AggroState);
            }
            else
            {
                if (_detectionTimer < _enemy.Settings.baseDetectionTime)
                    return;
                
                _enemy.TransitionToState(_enemy.AggroState);
            }

        }

        public void ExitState()
        {
            if (_waitCoroutine == null) return;
            _isWaiting = false;
            _enemy.StopCoroutine(_waitCoroutine);
        }
        
        public void OnCollisionEnter2D(Collision2D col)
        {
            if (((1 << col.gameObject.layer) & _enemy.playerLayer) != 0)
            {
                _enemy.TransitionToState(_enemy.AggroState);
            }
        }

        public void OnCollisionExit2D(Collision2D col) { }

        public void OnCollisionStay2D(Collision2D col) { }

        /*
        * Called if the enemy is detecting the player and they step out of view.
        * Instead of immediately returning to their patrol state the guard should keep looking in direction
        * of the player's light sighting for a short time before returning.
        */
        private IEnumerator WaitBeforeSwitchingBackToPatrol()
        {
            yield return new WaitForSeconds(2);

            if (_enemy.IsDetectingState())
            {
                _enemy.TransitionToState(_enemy.PatrollingState);
            }

            _isWaiting = false;
        }
    }
}