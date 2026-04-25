namespace VYRA.Core.History;

public sealed class HistoryLoadResult
{
    public HistoryLoadResult(HistoryDay today, IReadOnlyList<HistoryMessage> contextMessages)
    {
        Today = today;
        ContextMessages = contextMessages;
    }

    public HistoryDay Today { get; }
    public IReadOnlyList<HistoryMessage> ContextMessages { get; }
}
