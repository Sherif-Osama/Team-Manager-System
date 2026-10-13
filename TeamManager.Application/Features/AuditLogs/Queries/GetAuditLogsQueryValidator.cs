using FluentValidation;

namespace TeamManager.Application.Features.AuditLogs.Queries
{
    public sealed class GetAuditLogsQueryValidator : AbstractValidator<GetAuditLogsQuery>
    {
        public GetAuditLogsQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThan(0);

            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

            RuleFor(x => x.ToUtc).GreaterThanOrEqualTo(x => x.FromUtc).When(x => x.FromUtc.HasValue && x.ToUtc.HasValue);
        }
    }
}