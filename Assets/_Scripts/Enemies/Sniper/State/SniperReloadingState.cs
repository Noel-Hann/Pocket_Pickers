using System.Collections;
using _Scripts.Sound;
using UnityEngine;

namespace _Scripts.Enemies.Sniper.State
{
    public class SniperReloadingState : IEnemyState<SniperStateManager>
    {
        private SniperStateManager _enemy;
        private Coroutine _reloadCoroutine;
        public void EnterState(SniperStateManager enemy)
        {
            _enemy = enemy;
            _enemy.RayView.ignoreSweepAngle = false;
            _enemy.investigatingFalseTrigger = false;
            
            if ((_enemy.originallyFacingRight && !_enemy.Settings.isFacingRight) ||
                (!_enemy.originallyFacingRight && _enemy.Settings.isFacingRight))
                _enemy.Settings.FlipLocalScale();
            
            _reloadCoroutine = _enemy.StartCoroutine(Reload());
        }

        public void UpdateState() {}

        public void ExitState()
        {
            if (_reloadCoroutine is null) return;
            _enemy.StopCoroutine(_reloadCoroutine);
            _reloadCoroutine = null;
        }

        public void OnCollisionEnter2D(Collision2D col) {}

        public void OnCollisionStay2D(Collision2D col) {}
        public void OnCollisionExit2D(Collision2D col) { }

        private IEnumerator Reload()
        {
            yield return new WaitForSeconds(_enemy.Settings.reloadTime);
            
            EnemySoundManager.Instance.PlaySniperReloadedClip();
            // Default behavior is to switch back to patrolling directly after reloading to scan for the enemy again
            _enemy.TransitionToState(_enemy.PatrollingState);
        }
    }
}