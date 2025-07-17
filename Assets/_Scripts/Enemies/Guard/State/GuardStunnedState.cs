using System.Collections;
using _Scripts.Player;
using UnityEngine;

namespace _Scripts.Enemies.Guard.State
{
    public class GuardStunnedState : IEnemyState<GuardStateManager>
    {
        // TODO: Find a way to enable the collider for the card only so the enemy can still be disabled even in stun state
        private GuardStateManager _enemy;
        private Coroutine _stunCoroutine;
        private Collider2D _col;
        public void EnterState(GuardStateManager enemy)
        {
            _enemy = enemy;
            _enemy.StopMoving();
            _enemy.gameObject.GetComponent<EnemyAnimator>().disable();
            _stunCoroutine = _enemy.StartCoroutine(StunDuration());
        }

        public void UpdateState()
        {
            // TODO: play stunned animation
            _enemy.StopMoving();
            _enemy.Settings.HandleGroundDetection();
            _enemy.Settings.HandleGravity();
            if (_enemy.Settings.IsGrounded())
            {
                _enemy.StopFalling();
            }
            
            Physics2D.IgnoreCollision(PlayerVariables.Instance.Collider2D, _enemy.Collider2D);
        }

        public void ExitState()
        {
            _enemy.Rigidbody2D.isKinematic = false;
            _enemy.Collider2D.enabled = true;
            Physics2D.IgnoreCollision(PlayerVariables.Instance.Collider2D, _enemy.Collider2D, false);
            if (_stunCoroutine != null)
                _enemy.StopCoroutine(_stunCoroutine);
            _enemy.gameObject.GetComponent<EnemyAnimator>().endDisable();
        }

        public void OnCollisionEnter2D(Collision2D col)
        {
            Physics2D.IgnoreCollision(PlayerVariables.Instance.Collider2D, _enemy.Collider2D);
        }
        
        public void OnCollisionStay2D(Collision2D col) {}
        public void OnCollisionExit2D(Collision2D col) { }

        private IEnumerator StunDuration()
        {
            yield return new WaitForSeconds(_enemy.Settings.stunLength);

            // Transition back to Searching state
            _enemy.TransitionToState(_enemy.SearchingState);
        }
    }
}