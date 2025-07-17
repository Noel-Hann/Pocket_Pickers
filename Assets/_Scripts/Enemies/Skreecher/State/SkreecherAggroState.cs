using UnityEngine;

namespace _Scripts.Enemies.Skreecher.State
{
    public class SkreecherAggroState : IEnemyState<SkreecherStateManager>
    {
        private SkreecherStateManager _enemy;
        private Coroutine _screechCoroutine;
        public void EnterState(SkreecherStateManager enemy)
        {
            _enemy = enemy;
            _screechCoroutine = _enemy.StartCoroutine(_enemy.PerformScreech());

            _enemy.gameObject.GetComponent<EnemyAnimator>().chase();
            Debug.Log("Enter Aggro");

            _enemy.AlertAllEnemiesInRange();
        }

        public void UpdateState() { }

        public void ExitState()
        {
            if (_screechCoroutine is null) return;
            _enemy.StopCoroutine(_screechCoroutine);
            _screechCoroutine = null;
            _enemy.gameObject.GetComponent<EnemyAnimator>().stopChase();
        }

        public void OnCollisionEnter2D(Collision2D col) { }

        public void OnCollisionStay2D(Collision2D col) { }

        public void OnCollisionExit2D(Collision2D col) { }
    }
}