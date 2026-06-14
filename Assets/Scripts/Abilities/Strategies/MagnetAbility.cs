using UnityEngine;

namespace Abilities.Strategies
{
    /// <summary>
    /// Magnet ability: attracts nearby items to the player.
    /// </summary>
    public class MagnetAbility : AbilityStrategy
    {
        public override void Execute(AbilityExecutionContext context)
        {
            Debug.Log("Magnet ability executed - Implementation pending");
            // TODO: Implement magnet logic:
            // 1. Find all collectible items within range
            // 2. Animate them being pulled toward the collection point
            // 3. Award items to the player
        }
    }
}