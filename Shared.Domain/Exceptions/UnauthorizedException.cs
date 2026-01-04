namespace Shared.Domain.Exceptions
{
    public sealed class UnauthorizedException : DomainException
    {
        public UnauthorizedException(string message = "Unauthorized access")
            : base(message)
        {
        }
    }
}
