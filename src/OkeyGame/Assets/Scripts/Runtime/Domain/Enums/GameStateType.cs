namespace Runtime.Domain.Enums
{
    public enum GameStateType
    {
        None,
        Initializing,
        WaitingForPlayers,
        GameStarted,
        PlayerTurn,
        AITurn,
        RoundEnded,
        GameEnded,
        Paused
    }
}