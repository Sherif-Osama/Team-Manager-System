namespace TeamManager.Application.Abstractions.Persistence
{
    public interface IOutbox
    {
        void Add(string type, string payload);
    }
}