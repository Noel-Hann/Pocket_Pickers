using _Scripts.Enemies.Guard.State;
using UnityEngine;

namespace _Scripts.Enemies
{
    public interface IEnemyState<T> where T : class
    {
        // The 'Awake' function of the state, called on the frame the state is entered
        void EnterState(T enemy);
        // The 'Update' function of the state, called on every frame the state is active
        void UpdateState();
        // Called as cleanup before switching to another state in EnemyStateManager's TransitionToState function, mostly for stopping coroutines
        void ExitState();
        void OnCollisionEnter2D(Collision2D col);
        void OnCollisionStay2D(Collision2D col);
        void OnCollisionExit2D(Collision2D col);
    }
}