namespace TeamManager.Application.Common.Exceptions
{
    public class AccountLockedException : ApplicationExceptionBase
    {
        public AccountLockedException(string email, DateTime? lockedUntil) :
            base("Account temporarily locked. Try again later.")
        { }
    }
}