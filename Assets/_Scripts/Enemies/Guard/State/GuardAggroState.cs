using System;
using System.Collections;
using _Scripts.Card;
using _Scripts.Player;
using _Scripts.Player.State;
using _Scripts.Sound;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _Scripts.Enemies.Guard.State
{
    public class GuardAggroState : IEnemyState<GuardStateManager>
    {
        private GuardStateManager _enemy;
        private float _lastFlipTime = -Mathf.Infinity;
        private float _flipCooldown = 1f;
        private bool _movingToLastKnownPosition;
        private Vector2 _lastKnownPosition;
        private float _lastKnownLocationStartTime;
        private Coroutine _flipCoroutine;
        private Coroutine _timeAlertedBySkreecher;
        private bool _hasExecuted;
        private float _flipDelayDuration = 0.25f;
        private float _playerWidth;
        private bool _handlingTopCollision;

        public void EnterState(GuardStateManager enemy)
        {
            _enemy = enemy;
            _movingToLastKnownPosition = false;

            //set the animation
            _enemy.GetComponent<EnemyAnimator>().chase();
            
            if (_enemy.alertedFromAggroSkreecher)
            {
                _lastKnownPosition = PlayerVariables.Instance.transform.position;
                _timeAlertedBySkreecher = _enemy.StartCoroutine(TimeoutSkreecherAlert());
            }
        }

        public void UpdateState()
        {
            // Alerted by Skreecher movement handled separate from normal movement
            if (_enemy.alertedFromAggroSkreecher)
            {
                HandleAlertedBySkreecherMovement();
                return;
            }

            RunningIntoWallCheck();

            if (!_enemy.IsPlayerDetected())
            {
                // Player is no longer detected but enemy is already moving to last known location
                if (_movingToLastKnownPosition)
                {
                    // Aggro timeout to switch to searching state if the player has not been seen recently (if last known location was too far to reach)
                    if (!(Time.time - _lastKnownLocationStartTime >=
                          _enemy.Settings.checkLastKnownLocationTimeout)) return;
                    // Debug.Log("Movement timeout hit");
                    _movingToLastKnownPosition = false;
                    _enemy.StopMoving();
                    _enemy.TransitionToState(_enemy.SearchingState);
                    return;
                }

                // Player is no longer detected; start moving to last known position
                _enemy.StopMoving();
                _lastKnownPosition = PlayerVariables.Instance.transform.position;
                _movingToLastKnownPosition = true;
                _lastKnownLocationStartTime = Time.time; // Initialize timer
                MoveToLastKnownPosition();
                // Debug.Log("No detection");

                return;
            }

            // Player has been sighted this frame, move to their position

            var playerPosition = PlayerVariables.Instance.transform.position;
            var directionToPlayer = playerPosition - _enemy.transform.position;

            // Flip the enemy towards the player if necessary
            HandleFlip(directionToPlayer.x);

            if (_flipCoroutine != null) return;
            // Move towards the player
            var moveDirectionX = Mathf.Sign(directionToPlayer.x);
            var direction = new Vector2(moveDirectionX, 0).normalized;

            // Ensure proper sprite orientation
            if (PlayerVariables.Instance.transform.position.x > _enemy.transform.position.x &&
                !_enemy.Settings.isFacingRight)
            {
                _enemy.Settings.FlipLocalScale();
            }
            else if (PlayerVariables.Instance.transform.position.x < _enemy.transform.position.x &&
                     _enemy.Settings.isFacingRight)
            {
                _enemy.Settings.FlipLocalScale();
            }

            // ATTACKING ADDITION
            // if (Mathf.Abs(PlayerVariables.Instance.transform.position.x) - Mathf.Abs(_enemy.transform.position.x) <= 5f)
            // {
            //     _enemy.StopMoving();
            //     StartQteWithPlayer();
            //     return;
            // }

            _enemy.Move(direction, _enemy.Settings.aggroMovementSpeed);
        }

        public void ExitState()
        {
            // Cleanup coroutines on exit
            if (_flipCoroutine is not null)
            {
                _enemy.StopCoroutine(_flipCoroutine);
                _flipCoroutine = null;
            }

            if (_timeAlertedBySkreecher is not null)
            {
                _enemy.StopCoroutine(_timeAlertedBySkreecher);
                _timeAlertedBySkreecher = null;
                _enemy.alertedFromAggroSkreecher = false;
            }

            _hasExecuted = true;

            _enemy.gameObject.GetComponent<EnemyAnimator>().stopChase();
        }

        public void OnCollisionEnter2D(Collision2D col)
        {
            AggroCollision(col);
        }

        public void OnCollisionStay2D(Collision2D col)
        {
            AggroCollision(col);
        }

        public void OnCollisionExit2D(Collision2D col) {}

        private void AggroCollision(Collision2D col)
        {
            if (((1 << col.gameObject.layer) & _enemy.playerLayer) == 0) return;

            // Check if collision occurred from above
            var isCollisionFromAbove = false;

            foreach (ContactPoint2D contact in col.contacts)
            {
                // Debug.Log("Normal:" + contact.normal.y);

                if (Math.Abs(contact.normal.y - (-1)) < 0.1f)
                {
                    // Debug.Log("Top collision");
                    isCollisionFromAbove = true;
                    break;
                }
            }

            if (_handlingTopCollision) return;
            if (isCollisionFromAbove)
            {
                _handlingTopCollision = true;
                HandleCollisionFromAbove(col);
            }
            else
            {
                // TODO: Make state trans here
                AttackPlayer();
            }
        }

        private void HandleCollisionFromAbove(Collision2D col)
        {
            _playerWidth = col.collider.bounds.size.x;

            var isLeftBlocked = IsSideBlocked(Vector2.left);
            var isRightBlocked = IsSideBlocked(Vector2.right);

            if (isLeftBlocked && !isRightBlocked)
            {
                // Push player to the right, left side is blocked
                PushPlayerToSide(col.gameObject, Vector2.right);
            }
            else if (!isLeftBlocked && isRightBlocked)
            {
                // Push player to the left, right side is blocked
                PushPlayerToSide(col.gameObject, Vector2.left);
            }
            else if (isLeftBlocked && isRightBlocked)
            {
                // Both sides blocked, proceed with QTE
                _handlingTopCollision = false;
                AttackPlayer();
            }
            else
            {
                // Neither side blocked, determine player's side
                Vector2 playerOffset = col.transform.position - _enemy.transform.position;
                Vector2 pushDirection = playerOffset.x >= 0 ? Vector2.right : Vector2.left;
                PushPlayerToSide(col.gameObject, pushDirection);
            }
        }

        private bool IsSideBlocked(Vector2 direction)
        {
            var checkDistance = _playerWidth;
            var origin = _enemy.transform.position;
            var obstuctionLayers = _enemy.environmentLayer | _enemy.enemyLayer;

            var hit = Physics2D.Raycast(origin, direction, checkDistance, obstuctionLayers);
            return hit.collider != null;
        }

        private void PushPlayerToSide(GameObject player, Vector2 direction)
        {
            var pushDistance = _playerWidth; // Push distance equal to player's width
            var newPosition = _enemy.transform.position + (Vector3)(direction * pushDistance);

            // Move the player
            player.transform.position = new Vector2(newPosition.x, player.transform.position.y);

            AdjustFacingDirections(direction);

            // Proceed with QTE
            _handlingTopCollision = false;
            AttackPlayer();
        }

        // If the player and the enemy are not facing each other flip one or both of them around
        private void AdjustFacingDirections(Vector2 playerDirection)
        {
            if ((_enemy.Settings.isFacingRight && playerDirection == Vector2.left) ||
                (!_enemy.Settings.isFacingRight && playerDirection == Vector2.right))
            {
                _enemy.Settings.FlipLocalScale();
            }

            var playerIsFacingRight = PlayerVariables.Instance.isFacingRight;

            if ((playerIsFacingRight && playerDirection == Vector2.right) ||
                (!playerIsFacingRight && playerDirection == Vector2.left))
            {
                PlayerVariables.Instance.FlipLocalScale();
            }
        }

        private void MoveToLastKnownPosition()
        {
            // Flip the enemy towards the target position if necessary
            var directionToTarget = _lastKnownPosition - (Vector2)_enemy.transform.position;
            var needsToFlip = HandleFlip(directionToTarget.x);

            // Move towards the last known position unless flipping
            var moveDirectionX = Mathf.Sign(directionToTarget.x);
            var direction = new Vector2(moveDirectionX, 0).normalized;
            // if (needsToFlip && _flipCoroutine != null) direction = !_enemy.Settings.isFacingRight ? Vector2.right : Vector2.left;
            // else direction = _enemy.Settings.isFacingRight ? Vector2.right : Vector2.left;

            if (_flipCoroutine == null)
            {
                _enemy.Move(direction, _enemy.Settings.aggroMovementSpeed);
            }
            else
            {
                _enemy.StopMoving();
            }

            // Check if the enemy has reached the last known x position
            if (Mathf.Abs(_enemy.transform.position.x - _lastKnownPosition.x) <= 1.0f)
            {
                _enemy.StopMoving();
                _movingToLastKnownPosition = false;
                _enemy.TransitionToState(_enemy.SearchingState);
            }
        }

        // Check if the enemy is currently running into a wall and switch to searching state if they are
        private void RunningIntoWallCheck()
        {
            var direction = _enemy.Settings.isFacingRight ? Vector2.right : Vector2.left;
            var colliderBounds = _enemy.Collider2D.bounds;
            var origin = colliderBounds.center;
            origin.y = colliderBounds.min.y + colliderBounds.size.y * 0.75f;
            var hit = Physics2D.Raycast(origin, direction, 0.5f, _enemy.environmentLayer);
            Debug.DrawRay(origin, direction * 0.5f, Color.magenta);

            if (hit.collider == null) return;

            _enemy.StopMoving();
            _movingToLastKnownPosition = false;
            _enemy.TransitionToState(_enemy.SearchingState);
        }

        private void HandleAlertedBySkreecherMovement()
        {
            if (_enemy.IsPlayerDetected())
            {
                _enemy.alertedFromAggroSkreecher = false;
            }

            var targetPos = _lastKnownPosition;

            // Check if the enemy is running into a wall, if it is then turn around.
            var direction = _enemy.Settings.isFacingRight ? Vector2.right : Vector2.left;
            var colliderBounds = _enemy.Collider2D.bounds;
            var origin = colliderBounds.center;
            origin.y = colliderBounds.min.y + colliderBounds.size.y * 0.75f;
            var hit = Physics2D.Raycast(origin, direction, 0.5f, _enemy.environmentLayer);
            Debug.DrawRay(origin, direction * 0.5f, Color.magenta);

            if (hit.collider is not null)
            {
                _enemy.StopMoving();
                _enemy.Settings.FlipLocalScale();
                direction = _enemy.Settings.isFacingRight ? Vector2.right : Vector2.left;
                _enemy.Move(direction, _enemy.Settings.aggroMovementSpeed);
            }

            // If the enemy is above the player keep moving until a wall or ledge is hit
            if (targetPos.y < _enemy.transform.position.y)
            {
                direction = _enemy.Settings.isFacingRight ? Vector2.right : Vector2.left;
                _enemy.Move(direction, _enemy.Settings.aggroMovementSpeed);
            }
            else
            {
                var playerPos = PlayerVariables.Instance.transform.position;
                var enemyPos = _enemy.transform.position;
                var enemyFacingRight = _enemy.Settings.isFacingRight;
                if (playerPos.x > enemyPos.x && !enemyFacingRight && playerPos.y >= enemyPos.y)
                {
                    _enemy.Settings.FlipLocalScale();
                }
                else if (playerPos.x < enemyPos.x && enemyFacingRight && playerPos.y >= enemyPos.y)
                {
                    _enemy.Settings.FlipLocalScale();
                }

                direction = _enemy.Settings.isFacingRight ? Vector2.right : Vector2.left;
                _enemy.Move(direction, _enemy.Settings.aggroMovementSpeed);
            }
        }

        private bool HandleFlip(float directionToPlayerX)
        {
            var timeSinceLastFlip = Time.time - _lastFlipTime;
            if (timeSinceLastFlip < _flipCooldown)
                return false;

            var shouldFlip = (_enemy.Settings.isFacingRight && directionToPlayerX < -0.1f) ||
                             (!_enemy.Settings.isFacingRight && directionToPlayerX > 0.1f);

            if (shouldFlip && _flipCoroutine == null)
            {
                _flipCoroutine = _enemy.StartCoroutine(FlipAfterDelay(_flipDelayDuration));
                return true;
            }

            return false;
        }

        private void AttackPlayer()
        {
            // If the player is already in the QTE with this guard it shouldn't start again
            // _enemy.TransitionToState(_enemy.AttackingState);
            _enemy.TransitionToState(_enemy.StunnedState);
            PlayerStateManager.Instance.HandleHitByPatroller(!_enemy.Settings.IsFacingRight());
        }

        private IEnumerator TimeoutSkreecherAlert()
        {
            yield return new WaitForSeconds(_enemy.Settings.timeoutOfSkreecherAlert);
            _enemy.alertedFromAggroSkreecher = false;
            if (!_enemy.IsPlayerDetected())
                _enemy.TransitionToState(_enemy.SearchingState);
        }

        private IEnumerator FlipAfterDelay(float delay)
        {
            _enemy.StopMoving();

            yield return new WaitForSeconds(delay);

            _enemy.Settings.FlipLocalScale();
            _lastFlipTime = Time.time;
            _flipCoroutine = null;
        }
    }
}