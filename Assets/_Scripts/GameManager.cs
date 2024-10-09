using UnityEngine;

namespace _Scripts
{
    
    /*
     * The plan:
    V* 1. Add Sin prefab and Sin script
     * 1.5 Actually add multiple sin prefabs with different designs. They will all
     *      hold the sin tag, and run the same script. They will look different though
    V* 2. Sin script should contain a weight, or how much sin it holds
     * 3. Add prefab for potential sin location
    V* 3. Player should be able to pick up a sin object by touching it
     * 4. In GameManager, add a public variable for total remaining sin
     * 5. On loading, in GameManager, get a list of all Sin objects (including
     *      ones that were added during play) and put them into a list
     * 6. On loading, in game manager, get the sum of all sins currently in the vault, and
     *      store it in a public variable
    V* 7. In playerVariables, add 3 variables: sin held, sin accrued, and threshold
    V* 8. For sin held, when the player picks up a sin, add its weight to sin held.
     * 9.
     * 10. For sin accrued, when the player commits a sin such as killing, add to sin accrued
     * 11. For threshold, it should be a random value within a possible range.
     *      This should reset each time they create a new sin
     * 12. When the players sin accrued goes over the threshold, a potential sin should
     *      be chosen at random, and become a real sin with the weight of the players sin accrued
     * 13. When this happens, the player's sin accrued should be reset to 0, the player's threshold
     *      should be reset to a new random value, the newly created sin should be added to the list of
     *      sins, and its weight should be added to the total sin in game manager
     * 14. When the player picks up a sin, it should send out an event that is recieved by
     *      all of the enemies. They should then increase their aggro fov and distance based on
     *      the players sin held.
     * 15. Add a trigger location that when the player goes there, their sin held gets reset to 0
     *      This is a stand in for escaping the level and releasing their collected sins
     * 16. Have a static variable in GameManager that will keep track of a threshold of sin that
     *      is the amount to win
     * 17. When the player reaches the escape trigger section, it will run a check.
     *      It will run a recalculation of total sin remaining, to avoid any math errors
     *      it will check if the total sin remaining is less than the maximum escape sin
     *          if it is less, the player will win the game
     *          if it is not less, the player will not yet win the game
     */
    public class GameManager : MonoBehaviour
    {
        #region Singleton

        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType(typeof(GameManager)) as GameManager;

                return _instance;
            }
            set { _instance = value; }
        }

        private static GameManager _instance;

        #endregion
    }
}

