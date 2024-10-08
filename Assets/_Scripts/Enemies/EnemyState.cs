namespace _Scripts.Enemies
{
    public enum EnemyState
    {
        // States are shared by all enemy types, behavior in each state varies
        Patrolling, // Not aware of the player at all (eye closed)
        Detecting, // Sees the player, is not yet focused on them (eye opening)
        Aggro, // Fully aware of the player and their position (eye open)
        Searching, // Aware the player is nearby, unaware of the exact position (eye closing)
        Returning, // Pathfinding back to patrol point
        Stunned, // Affected by false trigger
        Disabled // Hit by card, dead or knocked out or something
    }
}