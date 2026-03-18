namespace Rivonix.EventFlow
{
    /// <summary>
    /// Base interface for all events in the Rivonix EventFlow system.
    /// All events should be structs for optimal performance (no garbage collection).
    /// </summary>
    public interface IEvent
    {
        // This is a marker interface - no methods required
        // Events are simple data containers
    }
}