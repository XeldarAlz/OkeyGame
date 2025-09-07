namespace Runtime.Domain.Enums
{
    public enum GameState
    {
        None,
        Initializing,
        WaitingForPlayers,
        GameStarted,
        PlayerTurn,
        AITurn,
        GameEnded,
        Paused
    }
}