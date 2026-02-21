namespace DungeonDescent;

class MessageLog
{
    private readonly Queue<string> _messages = new();
    private const int Capacity = 3;

    public void Add(string msg)
    {
        if (string.IsNullOrWhiteSpace(msg)) return;
        _messages.Enqueue(msg);
        while (_messages.Count > Capacity)
            _messages.Dequeue();
    }

    public IReadOnlyList<string> Lines => _messages.ToArray();
}
