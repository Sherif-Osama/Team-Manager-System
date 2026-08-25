using TeamManager.Domain.Common;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Domain.Entities;

public class Label : Entity<long>
{
    private readonly List<TaskLabel> _taskLabels = new();

    public Guid TeamId { get; private set; }
    public Team Team { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string ColorHex { get; private set; } = "#808080";
    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<TaskLabel> TaskLabels => _taskLabels.AsReadOnly();

    private Label()
    {
    }

    public Label(Guid teamId, string name, string? colorHex = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("A label must have a name.");

        TeamId = teamId;
        Name = name;
        if (!string.IsNullOrWhiteSpace(colorHex))
            ColorHex = colorHex;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("A label must have a name.");

        Name = name;
    }

    public void ChangeColor(string colorHex)
    {
        if (string.IsNullOrWhiteSpace(colorHex))
            throw new DomainException("A label color cannot be empty.");

        ColorHex = colorHex;
    }
}
