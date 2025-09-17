namespace Runtime.Domain.Enums
{
    public enum GameStateType : byte
    {
        None = 0,
        Initializing = 1,
        WaitingForPlayers = 2,
        GameStarted = 3,
        PlayerTurn = 4,
        AITurn = 5,
        RoundEnded = 6,
        GameEnded = 7,
        Paused = 8
    }
}