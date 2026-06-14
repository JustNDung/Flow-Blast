namespace Audio
{
    public readonly struct SoundSettingChangedMessage : MessageDispatcher.IMessage
    {
        public readonly bool Enabled;

        public SoundSettingChangedMessage(bool enabled)
        {
            Enabled = enabled;
        }
    }
}
