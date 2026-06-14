namespace Core
{
    /// <summary>
    /// Shared color group enum used by both ConveyorBelt and BoxConveyorBelt systems.
    /// Eliminates the duplicate ItemColorGroup definitions across the codebase.
    /// </summary>
    public enum ColorGroup
    {
        Red = 0,
        Yellow = 1,
        Blue = 2,
        Green = 3,
        Purple = 4,
        Orange = 5,
        Pink = 6,
        Cyan = 7
    }
}