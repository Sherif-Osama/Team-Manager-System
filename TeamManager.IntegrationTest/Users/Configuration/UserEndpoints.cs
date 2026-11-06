namespace TeamManager.IntegrationTest.Users.Configuration
{
    static class UserEndpoints
    {
        public const string Me = "/api/users/me";
        public const string MyEmail = "/api/users/me/email";
        public const string ConfirmMyEmail = "/api/users/me/email/confirm";
        public const string ResendMyEmailConfirmation = "/api/users/me/email/confirm/resend";
        public const string MyPassword = "/api/users/me/password";
        public const string DeactivateMe = "/api/users/me/deactivate";
    }
}
