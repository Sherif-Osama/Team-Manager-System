using Microsoft.Extensions.Options;
using TeamManager.Application.Abstractions.Configuration;

namespace TeamManager.Infrastructure.Services.AuthenticationServices
{
    public sealed class BootstrapSecretProvider(IOptions<BootstrapOptions> options) : IBootstrapSecretProvider
    {
        public string? AdminSecret => options.Value.AdminSecret;
    }
}