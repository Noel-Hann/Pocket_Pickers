using System.Collections;
using UnityEngine;

namespace _Scripts.Enemies.Guard.State
{
    public class GuardPatrollingState : IEnemyState<GuardStateManager>
    {
        private GuardStateManager _enemy;
        private Vector2 _originPosition;
        private Vector2 _leftPatrolPoint;
        private Vector2 _rightPatrolPoint;
        private Vector2 _currentTarget;
        private bool _isMovingRight;
        private bool _isWaiting;
        private Coroutine _waitCoroutine;
        public Vector2 LeftPatrolPoint() => _leftPatrolPoint;
        public Vector2 RightPatrolPoint() => _rightPatrolPoint;
        public Vector2 OriginPosition => _originPosition;
        public GuardPatrollingState(GuardStateManager enemy, Vector2 originPosition, float leftPatrolDistance, float rightPatrolDistance)
        {
            _enemy = enemy;
            _originPosition = originPosition;

            // Initialize patrol points
            _leftPatrolPoint = _originPosition + Vector2.left * leftPatrolDistance;
            _rightPatrolPoint = _originPosition + Vector2.right * rightPatrolDistance;
        }

        public void EnterState(GuardStateManager enemy)
        {
            _enemy = enemy;

            // Set initial direction and target based on facing direction
            if (_enemy.Settings.isFacingRight)
            {
                _isMovingRight = true;
                _currentTarget = _rightPatrolPoint;
            }
            else
            {
                _isMovingRight = false;
                _currentTarget = _leftPatrolPoint;
            }
        }

        public void UpdateState()
        {
            if (_enemy.IsPlayerDetected())
            {
                _enemy.TransitionToState(_enemy.DetectingState);
                return;
            }

            if (_isWaiting) return; 

            MoveTowardsTarget();
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
        
        public void OnCollisionStay2D(Collision2D col) {}

        private void MoveTowardsTarget()
        {
            var direction = _isMovingRight ? Vector2.right : Vector2.left;
            _enemy.Move(direction, _enemy.Settings.movementSpeed);

            if ((_isMovingRight && _enemy.transform.position.x >= _currentTarget.x) ||
                (!_isMovingRight && _enemy.transform.position.x <= _currentTarget.x))
            {
                _enemy.StopMoving();
                _isWaiting = true;
                _waitCoroutine = _enemy.StartCoroutine(WaitAtPoint());
            }
        }

        private IEnumerator WaitAtPoint()
        {
            yield return new WaitForSeconds(_enemy.Settings.waitTimeAtEnds);
            SwitchDirection();
            _isWaiting = false;
        }

        private void SwitchDirection()
        {
            _isMovingRight = !_isMovingRight;
            _enemy.Settings.FlipLocalScale();
            _currentTarget = _isMovingRight ? _rightPatrolPoint : _leftPatrolPoint;
        }

        public void OnCollisionExit2D(Collision2D col) { }

    }
}