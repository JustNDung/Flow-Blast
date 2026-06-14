using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Game
{
    [CreateAssetMenu(menuName = "Flow Blast/Level Config", fileName = "Level_", order = 1)]
    public class LevelConfig : ScriptableObject
    {
        [Header("Level Info")]
        [SerializeField] private int _levelNumber = 1;

        [Header("Conveyor Belt Color Sequence")]
        [SerializeField] private List<ColorGroup> _colorSequence = new()
        {
            ColorGroup.Red, ColorGroup.Red, ColorGroup.Red, ColorGroup.Red, ColorGroup.Red,
            ColorGroup.Blue, ColorGroup.Blue, ColorGroup.Blue, ColorGroup.Blue, ColorGroup.Blue,
            ColorGroup.Yellow, ColorGroup.Yellow, ColorGroup.Yellow, ColorGroup.Yellow, ColorGroup.Yellow,
            ColorGroup.Purple, ColorGroup.Purple, ColorGroup.Purple, ColorGroup.Purple, ColorGroup.Purple
        };

        [Header("Box Selection Panel")]
        [SerializeField] private List<ColorGroup> _availableBoxColors = new()
        {
            ColorGroup.Red,
            ColorGroup.Blue,
            ColorGroup.Yellow,
            ColorGroup.Purple
        };

        [Header("Abilities")]
        [SerializeField] private int _initialCoins = 300;
        [SerializeField] private int _magnetCount = 1;
        [SerializeField] private int _handCount = 1;
        [SerializeField] private int _shuffleCount = 1;

        public int LevelNumber => _levelNumber;
        public IReadOnlyList<ColorGroup> GetColorSequence() => _colorSequence;
        public IReadOnlyList<ColorGroup> GetAvailableBoxColors() => _availableBoxColors;
        public int InitialCoins => _initialCoins;
        public int MagnetCount => _magnetCount;
        public int HandCount => _handCount;
        public int ShuffleCount => _shuffleCount;
    }
}