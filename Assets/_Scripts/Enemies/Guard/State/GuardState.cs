namespace _Scripts.Enemies.Guard.State
{
    public enum GuardState
    {
        // THIS ENUM IS JUST FOR DEBUGGING IN THE INSPECTOR NOW, IT HAS NO IMPACT ON BEHAVIOR WHATSOEVER
        // ALL STATE LOGIC IS HANDLED IN THE ENEMY STATE MANAGER
        // States are shared by all enemy types, behavior in each state varies
        Patrolling, // Not aware of the player at all (eye closed)
        Detecting, // Sees the player, is not yet focused on them (eye opening)
        Aggro, // Fully aware of the player and their position (eye open)
        Attacking, // QTE behavior
        Searching, // Aware the player is nearby, unaware of the exact position (eye closing)
        Returning, // Pathfinding back to patrol point
        Investigating, // pathfinding to a false trigger
        Stunned, // Affected by false trigger
        Disabled // Hit by card, dead or knocked out or something
    }
}