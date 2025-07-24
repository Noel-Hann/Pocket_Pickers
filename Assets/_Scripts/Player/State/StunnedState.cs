using UnityEngine;

namespace _Scripts.Player.State
{
    public class StunnedState : IPlayerState
    {
        public void EnterState()
        {
            PlayerMovement.Instance.HaltHorizontalMomentum();
        }

        public void UpdateState()
        {
            PlayerMovement.Instance.HandleGravity();
            // PlayerMovement.Instance.AlterHorizontalMovement(0.95f);
        }

        public void FixedUpdateState()
        {
            // Apply gravity
            PlayerMovement.Instance.ApplyMovement();
        }
        public void ExitState()
        {
        }
    }
}