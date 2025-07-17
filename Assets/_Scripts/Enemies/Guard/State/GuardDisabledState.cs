using System.Collections;
using _Scripts.Player;
using UnityEngine;

namespace _Scripts.Enemies.Guard.State
{
    public class GuardDisabledState : IEnemyState<GuardStateManager>
    {
        private GuardStateManager _enemy;
        private Coroutine _timeout;
        public void EnterState(GuardStateManager enemy)
        {
            _enemy = enemy;
            _enemy.StopMoving();
            // _enemy.Rigidbody2D.isKinematic = true;
            Physics2D.IgnoreCollision(PlayerVariables.Instance.Collider2D, _enemy.Collider2D, true);
            /*if (Card.Card.Instance is not null)
            {
                Physics2D.IgnoreCollision(Card.Card.Instance.gameObject.GetComponent<Collider2D>(), _enemy.Collider2D, true);
            }*/
            // _enemy.transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, 90f)); // TODO: Change eventually
            _enemy.Settings.removeListeners();
            _enemy.gameObject.GetComponent<EnemyAnimator>().disable();

            _timeout = _enemy.StartCoroutine(Timeout());
        }

        public void UpdateState()
        {
            // TODO: Play disabled animation
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
            if (_timeout is not null)
            {
                _enemy.StopCoroutine(_timeout);
                _timeout = null;
            }

            _enemy.gameObject.GetComponent<EnemyAnimator>().endDisable();
            /*if (Card.Card.Instance is not null)
            {
                Physics2D.IgnoreCollision(Card.Card.Instance.gameObject.GetComponent<Collider2D>(), _enemy.Collider2D, false);
            }*/
            
            Physics2D.IgnoreCollision(PlayerVariables.Instance.Collider2D, _enemy.Collider2D, false);

        }

        public void OnCollisionEnter2D(Collision2D col)
        {
            Physics2D.IgnoreCollision(PlayerVariables.Instance.Collider2D, _enemy.Collider2D);
        }
        
        public void OnCollisionStay2D(Collision2D col) {}

        public void OnCollisionExit2D(Collision2D col) { }

        private IEnumerator Timeout()
        {
            yield return new WaitForSeconds(_enemy.Settings.disabledTimeout);
            _enemy.TransitionToState(_enemy.SearchingState);
        }
    }
}