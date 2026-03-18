namespace Rivonix.EventFlow
{
    /// <summary>
    /// A pipeline step that can mutate an event and decide whether execution continues.
    /// </summary>
    public delegate FlowResult EventStep<T>(ref T eventData) where T : IEvent;
}
