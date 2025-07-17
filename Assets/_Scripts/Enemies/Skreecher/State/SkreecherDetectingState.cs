using System.Collections;
using UnityEngine;

namespace _Scripts.Enemies.Skreecher.State
{
    public class SkreecherDetectingState : IEnemyState<SkreecherStateManager>
    {
        private SkreecherStateManager _enemy;
        private Coroutine _detectionCoroutine;
        private Coroutine _waitBeforePatrollTransitionCoroutine;
        public void EnterState(SkreecherStateManager enemy)
        {
            _enemy = enemy;
            _detectionCoroutine = _enemy.StartCoroutine(DetectionTimer());
            Debug.Log("Enter Detecting");
        }

        public void UpdateState()
        {
            if (_enemy.IsPlayerDetectedWithQuickDetect())
            {
                _enemy.TransitionToState(_enemy.AggroState);
            }
            
            if (!_enemy.IsPlayerDetected())
            {
                _enemy.StopCoroutine(_detectionCoroutine);
                _waitBeforePatrollTransitionCoroutine = _enemy.StartCoroutine(WaitBeforeTransition());
            }
            else
            {
                if (_waitBeforePatrollTransitionCoroutine is not null)
                {
                    _enemy.StopCoroutine(_waitBeforePatrollTransitionCoroutine);
                    _waitBeforePatrollTransitionCoroutine = null;
                }
            }
        }

        public void ExitState()
        {
            if (_detectionCoroutine is not null)
            {
                _enemy.StopCoroutine(_detectionCoroutine);
                _detectionCoroutine = null;
            }

            if (_waitBeforePatrollTransitionCoroutine is not null)
            {
                _enemy.StopCoroutine(_waitBeforePatrollTransitionCoroutine);
                _waitBeforePatrollTransitionCoroutine = null;
            }
        }

        public void OnCollisionEnter2D(Collision2D col) {}

        public void OnCollisionStay2D(Collision2D col) {}

        public void OnCollisionExit2D(Collision2D col) { }

        private IEnumerator DetectionTimer()
        {
            yield return new WaitForSeconds(_enemy.Settings.baseDetectionTime);
            _enemy.TransitionToState(_enemy.AggroState);
        }

        private IEnumerator WaitBeforeTransition()
        {
            yield return new WaitForSeconds(_enemy.Settings.waitBeforeTransitionTime);
            if (!_enemy.IsPlayerDetected())
                _enemy.TransitionToState(_enemy.PatrollingState);
        }
    }
}