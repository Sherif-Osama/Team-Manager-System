namespace TeamManager.Application.Common.Exceptions.UserExceptions
{
    public class AccountLockedException : ApplicationExceptionBase
    {
        public AccountLockedException(string email, DateTime? lockedUntil) :
            base("Account temporarily locked. Try again later.")
        { }
    }
}