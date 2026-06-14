namespace UI
{
    public readonly struct AbilityUsedMessage : MessageDispatcher.IMessage
    {
        public readonly AbilityType AbilityType;

        public AbilityUsedMessage(AbilityType abilityType)
        {
            AbilityType = abilityType;
        }
    }
}
