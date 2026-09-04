namespace TeamManager.Application.Abstractions.Configuration
{
    public interface IBootstrapSecretProvider
    {
        string? AdminSecret { get; }
    }
}