using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Card
{
    public class Card : MonoBehaviour
    {
        
        private float _startTime;

        //private Rigidbody2D _rigidbody;
        public InputHandler inputHandler;
        public static Card Instance {  get; private set; }

        private Vector2 direction; // Direction in which the card is launched
        public float speed = 15;
        private Vector2 velocity;
       // public Vector2 directon;

        /*
        The plan:
        Add a constant public speed, and a direction vector
        add a variable for total possible ricochets and a second variable
        for number of ricochets that have happend
        Make collision detection with tag recognition for four scenarios
            1. It hit a wall
                If we have done all our ricochets
                    we will switch to a falling state
                Otherwise
                    We will bounce off the wall and keep our speed (calculate
                    the normal of the wall, do an angle reflection calculation,
                    set that as the new direction, normalize that, and multiply by
                    speed to set new velocity)
            2. It hits the player
                Card goes through player, nothing happens
            3. Card hits a grate/bars
                Card goes through the bars, but the player cant go through the bars
            4. Card hits an enemy
                Card disapears, enemy is incapacitated, add sin
        */
        

        /*
        When this object first exists, get everything set up.
        When it gets destroyed, tear everything down.
        It is a prefab, so some things we can't set in the editor
        I am putting a lot of things in seperate functions, and then
        calling those functions, just to make it more readable.
        For example, I'm calling setListeners and deleteListeners,
        and those functions are seperate
        */

        
        private void OnEnable()
        {
            inputHandler = GameObject.Find("InputHandler").GetComponent<InputHandler>();
            setListeners();
        }

        private void OnDestroy()
        {
            deleteListeners();
        }
        
        private void setListeners()
        {
            inputHandler.OnEnterCardStance += DestroyCard;
            CardManager.Instance.Teleport += CatchTeleport;
        }

        //todo sometimes when the card gets deleted, it doesn't have the inputHandler item
        private void deleteListeners()
        {
            inputHandler.OnEnterCardStance -= DestroyCard;
            CardManager.Instance.Teleport -= CatchTeleport;
        }

        private void Awake()
        {
            
            //singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
            
            
            //_rigidbody = GetComponent<Rigidbody2D>();
            this.direction = HandleCardStanceArrow.Instance.currentDirection;
            _startTime = Time.time;
        }

        public void Launch(Vector2 direction)
        {
            this.direction = direction.normalized;
            calculateVelocity(this.direction);

            // Calculate initial velocity
            //var velocity = this.direction * CardManager.Instance.cardSpeed;

            // Apply velocity to the Rigidbody2D
            // _rigidbody.velocity = velocity;
        }

        private void calculateVelocity(Vector2 direction) {
            var velocity = direction * this.speed;
            this.velocity = velocity;
        }

        private void Update()
        {
            // Destroy the card after its lifetime expires
            if (Time.time - _startTime >= CardManager.Instance.cardLifeTime)
            {
                DestroyCard();
            }

            moveCard();
            // if (Input.GetButtonDown("CardThrow"))
            // {
            //     Debug.Log($"Activating teleporation using {transform.position.x}, {transform.position.y}");
            //     Teleport?.Invoke(new Vector2(transform.position.x, transform.position.y));
            //     DestroyCard();
            // }
        }

        private void moveCard()
        {
            Vector3 newPosition = ((Vector2)transform.position) + (velocity * Time.deltaTime);
            this.transform.position = newPosition;
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            // Ignore collisions with the player
            if (col.gameObject.CompareTag("Player"))
            {
                Physics2D.IgnoreCollision(col.collider, GetComponent<Collider2D>());
                return;
            } 
            // TODO: Add a check for walls and count bounces
            
            Debug.Log("Card collision");
        }

        //the teleport event needs to be caught by a function that takes in a vector2
        //all this function does is take in a vector 2 so that it can catch the Teleport
        //event, and then run DestroyCard.
        private void CatchTeleport(Vector2 noop)
        {
            Debug.Log("caught teleport");
            DestroyCard();
        }

        public void DestroyCard()
        {
            // Notify the CardManager that the card has been destroyed
            CardManager.Instance.OnCardDestroyed();
            Destroy(gameObject);
        }

        /*
         * the plan:
         * Have an onCollisionEnter function
         *  do a tag comparison to figure out what situation we're in
         *  Call a different function for each situation
         *  for now all the situations except a wall we will return
         * For a wall collision:
         *  check if bounces == totalBounces
         *      if it does, just switch states to fall (do later)
         *  Calculate the normal of the wall
         *  Calculate the reflection of the direction of the card
         *  against the normal of the wall
         *  call the function calculateVelocity with the new direction
         *  incriment bounces
         */
        
    }
}