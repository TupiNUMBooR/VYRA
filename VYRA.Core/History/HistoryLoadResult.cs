namespace VYRA.Core.History;

public sealed class HistoryLoadResult
{
    public HistoryLoadResult(
        HistoryDay today,
        IReadOnlyList<HistoryDay> loadedDaysForUi,
        IReadOnlyList<HistoryMessage> contextMessages)
    {
        Today = today;
        LoadedDaysForUi = loadedDaysForUi;
        ContextMessages = contextMessages;
    }

    public HistoryDay Today { get; }
    public IReadOnlyList<HistoryDay> LoadedDaysForUi { get; }
    public IReadOnlyList<HistoryMessage> ContextMessages { get; }
}
