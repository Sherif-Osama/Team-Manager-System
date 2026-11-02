namespace TeamManager.IntegrationTest.Authentication.Configuration
{
    static class AuthenticationEndpoints
    {
        public const string Register = "/api/authentication/register";
        public const string Login = "/api/authentication/login";
        public const string Refresh = "/api/authentication/refresh";
        public const string Logout = "/api/authentication/logout";
        public const string ForgotPassword = "/api/authentication/forgot-password";
    }
}