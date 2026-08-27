namespace TeamManager.Application.Common.Exceptions
{
    public class AccountLockedException : ApplicationExceptionBase
    {
        public AccountLockedException(string email, DateTime? lockedUntil) :
            base($"The account with email '{email}' is locked until {lockedUntil:yyyy-MM-dd HH:mm:ss}.")
        { }
    }
}