using _Scripts.Card;
using UnityEngine;

namespace _Scripts.Enemies.Guard.State
{
    public class GuardInvestigatingState : IEnemyState<GuardStateManager>
    {
        private GuardStateManager _enemy;
        private Vector2 _falseTriggerPos;

        public void EnterState(GuardStateManager enemy)
        {
            _enemy = enemy;
            _falseTriggerPos = CardManager.Instance.lastFalseTriggerPosition;

            if ((_enemy.Settings.IsFacingRight() && _falseTriggerPos.x < _enemy.transform.position.x) ||
                (!_enemy.Settings.IsFacingRight() && _falseTriggerPos.x > _enemy.transform.position.x))
                _enemy.Settings.FlipLocalScale();
        }

        public void UpdateState()
        {
            if (_enemy.IsPlayerDetected() || _enemy.IsPlayerDetectedWithQuickDetect())
                _enemy.TransitionToState(_enemy.AggroState);

            if ((_enemy.Settings.IsFacingRight() && _enemy.transform.position.x >= _falseTriggerPos.x) ||
                (!_enemy.Settings.IsFacingRight() && _enemy.transform.position.x <= _falseTriggerPos.x) ||
                !_enemy.Settings.ledgeCheck.IsTouchingLayers(_enemy.environmentLayer))
            {
                _enemy.StopMoving();
                _enemy.Rigidbody2D.constraints = RigidbodyConstraints2D.FreezePositionX;
                _enemy.TransitionToState(_enemy.SearchingState);
                return;
            }

            var direction = (_falseTriggerPos - (Vector2)_enemy.transform.position).normalized;
            _enemy.Move(direction, _enemy.Settings.movementSpeed);
        }

        public void ExitState()
        {
            _enemy.Rigidbody2D.constraints = RigidbodyConstraints2D.None;
            _enemy.Rigidbody2D.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        public void OnCollisionEnter2D(Collision2D col)
        {
            _enemy.TransitionToState(_enemy.AggroState);
        }

        public void OnCollisionStay2D(Collision2D col)
        {
            _enemy.TransitionToState(_enemy.AggroState);
        }

        public void OnCollisionExit2D(Collision2D col) { }
    }
}