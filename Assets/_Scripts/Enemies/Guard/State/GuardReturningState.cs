using UnityEngine;

namespace _Scripts.Enemies.Guard.State
{
    public class GuardReturningState : IEnemyState<GuardStateManager>
    {
        private GuardStateManager _enemy;

        public void EnterState(GuardStateManager enemy)
        {
            _enemy = enemy;
        }

        public void UpdateState()
        {
            var originPosition = _enemy.transform.position;
            var distanceToOrigin = _enemy.Settings.patrolOrigin - originPosition.x;
            var direction = distanceToOrigin > 0 ? Vector2.right : Vector2.left;

            // Flip towards the origin if necessary
            if ((_enemy.Settings.isFacingRight && direction == Vector2.left) ||
                (!_enemy.Settings.isFacingRight && direction == Vector2.right))
            {
                _enemy.Settings.FlipLocalScale();
            }

            // Move towards the origin position
            _enemy.Move(direction, _enemy.Settings.movementSpeed);

            if (Mathf.Abs(distanceToOrigin) <= 0.1f)
            {
                _enemy.StopMoving();
                _enemy.TransitionToState(_enemy.PatrollingState);
            }
        }

        public void ExitState()
        {
            return;
        }

        public void OnCollisionEnter2D(Collision2D col)
        {
            if (((1 << col.gameObject.layer) & _enemy.playerLayer) != 0)
            {
                _enemy.TransitionToState(_enemy.AggroState);
            }
        }

        public void OnCollisionStay2D(Collision2D col) { }

        public void OnCollisionExit2D(Collision2D col) { }
    }
}