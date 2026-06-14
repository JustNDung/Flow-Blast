using UnityEngine;

namespace Abilities.Strategies
{
    /// <summary>
    /// Hand ability: removes a selected item from the board.
    /// </summary>
    public class HandAbility : AbilityStrategy
    {
        public override void Execute(AbilityExecutionContext context)
        {
            Debug.Log("Hand ability executed - Implementation pending");
            // TODO: Implement hand logic:
            // 1. Enter selection mode for the player to pick an item to remove
            // 2. Remove the selected item from the board
            // 3. Grant resources or clear space
        }
    }
}