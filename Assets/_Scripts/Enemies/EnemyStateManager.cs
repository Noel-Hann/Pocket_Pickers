using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Scripts.Enemies
{
    public class EnemyStateManager : MonoBehaviour
    {
        public EnemyState state;
        private IViewType _view;
        
        private void Awake()
        {
            _view = GetComponent<IViewType>();
        }

        private void OnEnable()
        {
            _view.PlayerDetected += HandlePlayerSighting;
            _view.NoPlayerDetected += HandleNoPlayerSighting;
        }

        private void OnDisable()
        {
            _view.PlayerDetected -= HandlePlayerSighting;
            _view.NoPlayerDetected -= HandleNoPlayerSighting;
        }

        private void HandlePlayerSighting()
        {
            switch (state)
            {
                case EnemyState.Patrolling:
                    state = EnemyState.Detecting;
                    return;
                case EnemyState.Detecting:
                    return;
                case EnemyState.Agro:
                    return;
                case EnemyState.Searching:
                    state = EnemyState.Agro;
                    return;
                case EnemyState.Stunned:
                    return;
            }
        }

        private void HandleNoPlayerSighting()
        {
            Debug.Log("No Player Sighted");
            switch (state)
            {
                case EnemyState.Patrolling:
                    return;
                case EnemyState.Detecting:
                    state = EnemyState.Patrolling;
                    return;
                case EnemyState.Agro:
                    state = EnemyState.Searching;
                    return;
                case EnemyState.Searching:
                    return;
                case EnemyState.Stunned:
                    return;
            }
        }
    }
}