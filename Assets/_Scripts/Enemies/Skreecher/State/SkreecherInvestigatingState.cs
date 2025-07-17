using UnityEngine;

namespace _Scripts.Enemies.Skreecher.State
{
    public class SkreecherInvestigatingState : IEnemyState<SkreecherStateManager>
    {
        private SkreecherStateManager _enemy;
        private Coroutine _screechCoroutine;
        public void EnterState(SkreecherStateManager enemy)
        {
            _enemy = enemy;
            _screechCoroutine = _enemy.StartCoroutine(_enemy.PerformScreech());
            
            Debug.Log("Enter Investigating");
            
            _enemy.AlertAllEnemiesInRange();
        }

        public void UpdateState() {}

        public void ExitState()
        {
            if (_screechCoroutine is null) return;
            _enemy.StopCoroutine(_screechCoroutine);
            _screechCoroutine = null;
        }

        public void OnCollisionEnter2D(Collision2D col) {}

        public void OnCollisionStay2D(Collision2D col) {}
        public void OnCollisionExit2D(Collision2D col) { }
    }
}