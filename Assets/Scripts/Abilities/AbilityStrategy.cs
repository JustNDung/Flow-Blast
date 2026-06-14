namespace Abilities
{
    /// <summary>
    /// Base class for all ability execution strategies.
    /// Subclasses define the specific behavior when an ability is activated.
    /// </summary>
    public abstract class AbilityStrategy
    {
        /// <summary>
        /// Execute the ability's effect in the game world.
        /// </summary>
        /// <param name="context">Runtime context with references to game systems.</param>
        public abstract void Execute(AbilityExecutionContext context);

        /// <summary>
        /// Optional: Called when ability execution fails (e.g., no valid targets).
        /// Can be used to restore the ability count.
        /// </summary>
        public virtual void OnExecutionFailed(AbilityExecutionContext context) { }
    }
}