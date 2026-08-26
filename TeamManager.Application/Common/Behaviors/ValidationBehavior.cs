using FluentValidation;
using MediatR;

namespace TeamManager.Application.Common.Behaviors
{
    public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (_validators.Any())
            {
                var context = new ValidationContext<TRequest>(request);

                var validationResults = await Task.WhenAll(
                    _validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

                var errors = validationResults.SelectMany(result => result.Errors).Where(error => error is not null).ToList();

                if (errors.Count != 0)
                {
                    throw new ValidationException(errors);
                }
            }

            return await next();
        }
    }
}