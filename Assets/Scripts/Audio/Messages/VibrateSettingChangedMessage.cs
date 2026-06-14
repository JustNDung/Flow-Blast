namespace Audio
{
    public readonly struct VibrateSettingChangedMessage : MessageDispatcher.IMessage
    {
        public readonly bool Enabled;

        public VibrateSettingChangedMessage(bool enabled)
        {
            Enabled = enabled;
        }
    }
}
