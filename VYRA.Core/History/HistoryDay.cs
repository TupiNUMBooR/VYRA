namespace VYRA.Core.History;

public sealed class HistoryDay
{
    public HistoryDay(DateTime date, IReadOnlyList<HistoryMessage> messages)
    {
        Date = date.Date;
        Messages = messages;
    }

    public DateTime Date { get; }
    public IReadOnlyList<HistoryMessage> Messages { get; }
}
