using UnityEngine;

namespace Abilities
{
    [CreateAssetMenu(menuName = "Flow Blast/Ability Definition", fileName = "Ability_", order = 10)]
    public class AbilityDefinition : ScriptableObject
    {
        [SerializeField] private string _abilityName;
        [SerializeField] private string _description;
        [SerializeField] private AbilityType _abilityType;
        [SerializeField] private int _defaultCount = 1;
        [SerializeField] private Sprite _icon;
        [SerializeField] private string _uiContainerName;
        [SerializeField] private string _uiButtonName;
        [SerializeField] private string _uiCountLabelName;

        public string AbilityName => _abilityName;
        public string Description => _description;
        public AbilityType AbilityType => _abilityType;
        public int DefaultCount => _defaultCount;
        public Sprite Icon => _icon;
        public string UIContainerName => _uiContainerName;
        public string UIButtonName => _uiButtonName;
        public string UICountLabelName => _uiCountLabelName;
    }

    public enum AbilityType
    {
        Magnet,
        Hand,
        Shuffle
    }
}
