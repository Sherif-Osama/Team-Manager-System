using System;
using System.Collections.Generic;
using System.Text;

namespace TeamManager.IntegrationTest.Authentication
{
    static class AuthenticationEndpoints
    {
        public const string Register = "/api/authentication/register";
        public const string Login = "/api/authentication/login";
        public const string Refresh = "/api/authentication/refresh";
    }
}