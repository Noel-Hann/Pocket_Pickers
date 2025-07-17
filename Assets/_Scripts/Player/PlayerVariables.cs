using System;
using _Scripts.Player.State;
using UnityEngine;
using UnityEngine.Timeline;
using Random = UnityEngine.Random;

namespace _Scripts.Player
{
    public class PlayerVariables : MonoBehaviour
    {
        public bool isFacingRight = true;   // Start facing right by default

        // public bool inCardStance;
        [HideInInspector] public PlayerStateManager stateManager;
        [HideInInspector] public BoxCollider2D Collider2D;
        [HideInInspector] public Rigidbody2D RigidBody2D; 
        [SerializeField] public ScriptableStats Stats;

        [HideInInspector] public float Time;

        [Header("Movement Options")] 
        public bool isDashEnabled = true;
        public bool isWallClimbEnabled = true;
        
        //sin variables
        public int sinHeld;//how much you have picked up
        public int sinAccrued;//how much sin you have commited in game
        public int thresholdRangeStart;
        public int thresholdRangeEnd;
        
        public int sinThreshold;//how much sinAccrued you have to reach in order to release a new sin
        public int currentHealth { get; set; } = 3;
        
        public void CollectSin(int weight)
        {
            Debug.Log("Collected sin of weight " + weight);
            sinHeld += weight;
            Debug.Log("Total sin collected: " + sinHeld);
        }

        public void CommitSin(int weight)
        {
            Debug.Log("Committed sin of weight " + weight);
            this.sinAccrued += weight;
            Debug.Log("Total sin commited: " + this.sinAccrued);
            if (sinAccrued >= sinThreshold)
            {
                Debug.Log("Should add a new sin to the bank vault");
                GameManager.Instance.releaseSin(sinAccrued);
                sinAccrued = 0;
                sinThreshold = Random.Range(thresholdRangeStart, thresholdRangeEnd);
            }
        }

        #region Singleton

        public static PlayerVariables Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType(typeof(PlayerVariables)) as PlayerVariables;

                return _instance;
            }
            set
            {
                _instance = value;
            }
        }
        private static PlayerVariables _instance;
        #endregion
        
        public void FlipLocalScale()
        {
            var localScale = transform.localScale;
            localScale.x *= -1;
            transform.localScale = localScale;
        }

        private void Awake()
        {
            stateManager = GetComponent<PlayerStateManager>();
            Collider2D = GetComponent<BoxCollider2D>();
            RigidBody2D = GetComponent<Rigidbody2D>();
            sinThreshold = Random.Range(thresholdRangeStart, thresholdRangeEnd);
        }

        private void Update()
        {
            Time = UnityEngine.Time.time;
            isFacingRight = transform.localScale.x > 0; 
        }

        // public void Escape()
        // {
        //     GameManager.Instance.EscapeLevel();
        // }
    }
}
