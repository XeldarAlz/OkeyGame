namespace Runtime.Domain.Enums
{
    public enum ValidationResult : byte
    {
        Valid = 0,
        Invalid = 1,
        InvalidMove = 2,
        InvalidSet = 3,
        InvalidSequence = 4,
        InsufficientTiles = 5,
    }
}
