using System.Collections;
using UnityEngine;

namespace _Scripts.Enemies.Guard.State
{
    public class GuardSearchingState : IEnemyState<GuardStateManager>
    {
        private GuardStateManager _enemy;
        private float _searchTimer;
        private bool _isSearching;
        private Coroutine _searchCoroutine;

        public void EnterState(GuardStateManager enemy)
        {
            _enemy = enemy;
            _enemy.Rigidbody2D.isKinematic = false;
            _enemy.Collider2D.enabled = true;
            _enemy.StopMoving();
            _searchTimer = 0f;
            _isSearching = true;
            _searchCoroutine = _enemy.StartCoroutine(SearchRoutine());
        }

        public void UpdateState()
        {
            if (_enemy.IsPlayerDetected())
            {
                _enemy.TransitionToState(_enemy.AggroState);
            }
        }

        public void ExitState()
        {
            if (_searchCoroutine != null)
                _enemy.StopCoroutine(_searchCoroutine);
            _isSearching = false;
        }

        public void OnCollisionEnter2D(Collision2D col)
        {
            if (((1 << col.gameObject.layer) & _enemy.playerLayer) != 0)
            {
                _enemy.TransitionToState(_enemy.AggroState);
            }
        }
        
        public void OnCollisionStay2D(Collision2D col) {}
        public void OnCollisionExit2D(Collision2D col) { }

        private IEnumerator SearchRoutine()
        {
            while (_searchTimer < _enemy.Settings.totalSearchTime && _isSearching)
            {
                if (_enemy.IsPlayerDetected())
                {
                    _enemy.TransitionToState(_enemy.AggroState);
                    yield break;
                }
                // Flip the enemy to look in the other direction
                _enemy.Settings.FlipLocalScale();

                // Wait for the search interval
                yield return new WaitForSeconds(_enemy.Settings.searchIntervalTime);
                _searchTimer += _enemy.Settings.searchIntervalTime;
            }

            if (_isSearching)
            {
                _enemy.TransitionToState(_enemy.ReturningState);
            }
        }
    }
}