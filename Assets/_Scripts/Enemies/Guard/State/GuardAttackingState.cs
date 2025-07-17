using System.Collections;
using _Scripts.Player;
using _Scripts.Player.State;
using UnityEditor;
using UnityEngine;

namespace _Scripts.Enemies.Guard.State
{
    public class GuardAttackingState : IEnemyState<GuardStateManager>
    {
        private Coroutine _standCoroutine;
        private Coroutine _chargeCoroutine;
        private GuardStateManager _enemy;
        public void EnterState(GuardStateManager enemy)
        {
            _enemy = enemy;
            // _enemy.gameObject.GetComponent<EnemyAnimator>().disable();
            _standCoroutine = _enemy.StartCoroutine(StandStill());
            _chargeCoroutine  = _enemy.StartCoroutine(Charge());
        }

        public void UpdateState() {}

        public void ExitState()
        {
            if (_standCoroutine != null)
            {
                _enemy.StopCoroutine(_standCoroutine);
                _standCoroutine = null;
            }
            if (_chargeCoroutine != null)
            {
                _enemy.StopCoroutine(_chargeCoroutine);
                _chargeCoroutine = null;
            }
        }

        public void OnCollisionEnter2D(Collision2D col) {}
        public void OnCollisionExit2D(Collision2D col)
        {
            if (((1 << col.gameObject.layer) & _enemy.playerLayer) == 0)
                return;

            PlayerVariables.Instance.currentHealth--;

            if (PlayerVariables.Instance.currentHealth <= 0)
            {
                GameManager.Instance.Die();
            }

            Debug.Log("Player Hit by attack");
            _enemy.TransitionToState(_enemy.StunnedState);
        }

        public void OnCollisionStay2D(Collision2D col)
        { 
        }

        public IEnumerator StandStill()
        {
            _enemy.StopMoving();
            yield return new WaitForSeconds(0.1f);
        }

        public IEnumerator Charge()
        {
            _enemy.DashForward();
            yield return new WaitForSeconds(0.2f);
            _enemy.TransitionToState(_enemy.SearchingState);
        }

    }
}