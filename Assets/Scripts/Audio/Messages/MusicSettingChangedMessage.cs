namespace Audio
{
    public readonly struct MusicSettingChangedMessage : MessageDispatcher.IMessage
    {
        public readonly bool Enabled;

        public MusicSettingChangedMessage(bool enabled)
        {
            Enabled = enabled;
        }
    }
}
