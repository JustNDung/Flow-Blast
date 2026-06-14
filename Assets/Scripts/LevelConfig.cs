using System.Collections.Generic;
using Abilities;
using ConveyorBelt;
using UnityEngine;

namespace Game
{
    [CreateAssetMenu(menuName = "Flow Blast/Level Config", fileName = "Level_", order = 1)]
    public class LevelConfig : ScriptableObject
    {
        [Header("Level Info")]
        [SerializeField] private int _levelNumber = 1;

        [Header("Conveyor Belt")]
        [SerializeField] private List<ConveyorBelt.ConveyorBelt.ItemColorGroup> _colorSequence = new()
        {
            ConveyorBelt.ConveyorBelt.ItemColorGroup.Red,
            ConveyorBelt.ConveyorBelt.ItemColorGroup.Red,
            ConveyorBelt.ConveyorBelt.ItemColorGroup.Red,
            ConveyorBelt.ConveyorBelt.ItemColorGroup.Red,
            ConveyorBelt.ConveyorBelt.ItemColorGroup.Red,
            ConveyorBelt.ConveyorBelt.ItemColorGroup.Blue,
            ConveyorBelt.ConveyorBelt.ItemColorGroup.Blue,
            ConveyorBelt.ConveyorBelt.ItemColorGroup.Blue,
            ConveyorBelt.ConveyorBelt.ItemColorGroup.Blue,
            ConveyorBelt.ConveyorBelt.ItemColorGroup.Blue,
            ConveyorBelt.ConveyorBelt.ItemColorGroup.Yellow,
            ConveyorBelt.ConveyorBelt.ItemColorGroup.Yellow,
            ConveyorBelt.ConveyorBelt.ItemColorGroup.Yellow,
            ConveyorBelt.ConveyorBelt.ItemColorGroup.Yellow,
            ConveyorBelt.ConveyorBelt.ItemColorGroup.Yellow,
            ConveyorBelt.ConveyorBelt.ItemColorGroup.Purple,
            ConveyorBelt.ConveyorBelt.ItemColorGroup.Purple,
            ConveyorBelt.ConveyorBelt.ItemColorGroup.Purple,
            ConveyorBelt.ConveyorBelt.ItemColorGroup.Purple,
            ConveyorBelt.ConveyorBelt.ItemColorGroup.Purple
        };

        [Header("Box Selection Panel")]
        [SerializeField] private List<BoxConveyorBelt.ItemColorGroup> _availableBoxColors = new()
        {
            BoxConveyorBelt.ItemColorGroup.Red,
            BoxConveyorBelt.ItemColorGroup.Blue,
            BoxConveyorBelt.ItemColorGroup.Yellow,
            BoxConveyorBelt.ItemColorGroup.Purple
        };

        [Header("Abilities")]
        [SerializeField] private int _initialCoins = 300;
        [SerializeField] private int _magnetCount = 1;
        [SerializeField] private int _handCount = 1;
        [SerializeField] private int _shuffleCount = 1;

        public int LevelNumber => _levelNumber;
        public IReadOnlyList<ConveyorBelt.ConveyorBelt.ItemColorGroup> ColorSequence => _colorSequence;
        public IReadOnlyList<BoxConveyorBelt.ItemColorGroup> AvailableBoxColors => _availableBoxColors;
        public int InitialCoins => _initialCoins;
        public int MagnetCount => _magnetCount;
        public int HandCount => _handCount;
        public int ShuffleCount => _shuffleCount;
    }
}
