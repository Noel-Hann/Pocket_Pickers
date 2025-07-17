using System.Collections;
using _Scripts.Sound;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts.Enemies.Sniper.State
{
    public class SniperChargingState : IEnemyState<SniperStateManager>
    {
        private SniperStateManager _enemy;
        private Coroutine _chargeCoroutine;
        public void EnterState(SniperStateManager enemy)
        {
            Debug.Log("Enter Charging State");
            _enemy = enemy;
            _chargeCoroutine = _enemy.StartCoroutine(ChargeShot());
        }

        public void UpdateState() { }

        public void ExitState()
        {
            if (_chargeCoroutine is not null)
            {
                _enemy.StopCoroutine(_chargeCoroutine);
                _chargeCoroutine = null;
            }
        }

        public void OnCollisionEnter2D(Collision2D col) { }

        public void OnCollisionStay2D(Collision2D col) { }

        private IEnumerator ChargeShot()
        {
            var chargeTime = _enemy.Settings.chargeTime;

            var duration = chargeTime * 0.66f;
            _enemy.RayView.ChangeToColor(new Color(1f, 0.5f, 0f), duration); // RGB for orange
            yield return new WaitForSeconds(duration);

            duration = chargeTime * 0.33f;
            _enemy.RayView.ChangeToColor(Color.red, duration);
            yield return new WaitForSeconds(duration);

            duration = 0.15f;
            _enemy.RayView.ChangeToColor(Color.white, duration);
            yield return new WaitForSeconds(duration);

            FireShot();
        }

        private void FireShot()
        {
            EnemySoundManager.Instance.PlaySniperShotClip();

            if (_enemy.IsPlayerDetected())
            {
                // TODO: Eventually this will need to call a full cleanup of the level. For now just restart the scene
                //SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                GameManager.Instance.Die();
                return;
            }

            // Kill enemies that are in the sniper's RayView
            if (_enemy.RayView.EnemiesDetected().Count != 0)
            {
                Debug.Log("Enemies in sniper's path: " + _enemy.RayView.EnemiesDetected().Count);
                foreach (var enemy in _enemy.RayView.EnemiesDetected())
                    enemy.GetComponent<IEnemyStateManagerBase>().KillEnemyWithoutGeneratingSin();
            }

            _enemy.RayView.ResetLineRendererColor();

            Debug.Log("Switching to reload");
            _enemy.TransitionToState(_enemy.ReloadingState);
        }

        public void OnCollisionExit2D(Collision2D col) { }
    }
}