namespace UI
{
    public enum GameResult
    {
        Win,
        Lose
    }

    public readonly struct GameStateMessage : MessageDispatcher.IMessage
    {
        public readonly GameResult Result;

        public GameStateMessage(GameResult result)
        {
            Result = result;
        }
    }
}