using UnityEngine;

namespace Abilities.Strategies
{
    /// <summary>
    /// Shuffle ability: randomizes the positions of all items on the board.
    /// </summary>
    public class ShuffleAbility : AbilityStrategy
    {
        public override void Execute(AbilityExecutionContext context)
        {
            Debug.Log("Shuffle ability executed - Implementation pending");
            // TODO: Implement shuffle logic:
            // 1. Collect all items currently on the board
            // 2. Randomize their positions
            // 3. Animate the shuffle effect
        }
    }
}