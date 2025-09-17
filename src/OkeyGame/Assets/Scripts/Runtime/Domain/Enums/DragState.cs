namespace Runtime.Domain.Enums
{
    public enum DragState : byte
    {
        None = 0,
        Selected = 1,
        Dragging = 2,
        Hovering = 3,
        Snapping = 4,
        Dropped = 5
    }
}