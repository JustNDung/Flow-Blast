namespace Abilities
{
    /// <summary>
    /// Provides runtime context for ability execution.
    /// Holds references to game systems that abilities can interact with.
    /// Extend this class with additional references as needed.
    /// </summary>
    public class AbilityExecutionContext
    {
        // Future references to game systems go here, for example:
        // public BoardManager Board { get; }
        // public BallManager BallManager { get; }
        // public ConveyorBeltController ConveyorBelt { get; }

        public AbilityExecutionContext()
        {
        }
    }
}