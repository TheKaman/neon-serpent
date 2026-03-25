namespace NeonSerpent.Food
{
    /// <summary>Types of food that can appear on the grid.</summary>
    public enum FoodType
    {
        Normal,  // always present, standard points
        Bonus,   // high points, timed despawn
        Poison   // applies PoisonEffect on eat
    }
}
