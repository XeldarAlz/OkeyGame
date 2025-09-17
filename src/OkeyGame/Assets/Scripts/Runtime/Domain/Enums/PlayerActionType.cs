namespace Runtime.Domain.Enums
{
    public enum PlayerActionType : byte
    {
        None = 0,
        DrawFromPile = 1,
        DrawFromDiscard = 2,
        DiscardTile = 3,
        MoveTileInRack = 4,
        DeclareWin = 5,
        ShowIndicator = 6,
    }
}
