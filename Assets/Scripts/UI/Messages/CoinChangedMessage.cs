namespace UI
{
    public readonly struct CoinChangedMessage : MessageDispatcher.IMessage
    {
        public readonly int Amount;
        public readonly int TotalCoins;

        public CoinChangedMessage(int amount, int totalCoins)
        {
            Amount = amount;
            TotalCoins = totalCoins;
        }
    }
}
