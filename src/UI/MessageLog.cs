namespace DungeonDescent;

class MessageLog
{
    // Maximum number of recent lines retained. Must equal the row count
    // of the log surface (see Layout.LogHeight) so the renderer never
    // has to truncate or pad against a different bound.
    public const int Capacity = 3;

    private readonly Queue<string> _messages = new();

    public void Add(string msg)
    {
        if (string.IsNullOrWhiteSpace(msg)) return;
        _messages.Enqueue(msg);
        while (_messages.Count > Capacity)
            _messages.Dequeue();
    }

    public IReadOnlyList<string> Lines => _messages.ToArray();
}
