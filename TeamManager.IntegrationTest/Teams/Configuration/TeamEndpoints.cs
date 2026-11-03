namespace TeamManager.IntegrationTest.Teams.Configuration
{
    static class TeamEndpoints
    {
        public const string Teams = "/api/teams";
        public const string TeamsByName = "/api/teams/by-name";
        public const string AcceptInvitation = "/api/teaminvitations/accept";
        public const string RejectInvitation = "/api/teaminvitations/reject";
        public const string InvitationsByEmail = "/api/teaminvitations/by-email";
        public const string MyInvitations = "/api/teaminvitations/my";

        public static string Team(Guid teamId) => $"/api/teams/{teamId}";
        public static string TeamOwner(Guid teamId) => $"/api/teams/{teamId}/owner";
        public static string TeamDeactivate(Guid teamId) => $"/api/teams/{teamId}/deactivate";
        public static string TeamActivate(Guid teamId) => $"/api/teams/{teamId}/activate";
        public static string TeamActivity(Guid teamId) => $"/api/teams/{teamId}/activity";

        public static string Labels(Guid teamId) => $"/api/teams/{teamId}/labels";
        public static string Label(Guid teamId, long labelId) => $"/api/teams/{teamId}/labels/{labelId}";

        public static string Members(Guid teamId) => $"/api/teammembers/{teamId}/members";
        public static string Member(Guid teamId, long memberId) => $"/api/teammembers/{teamId}/members/{memberId}";
        public static string MemberRole(Guid teamId, long memberId) => $"/api/teammembers/{teamId}/members/{memberId}/role";

        public static string Invitations(Guid teamId) => $"/api/teaminvitations/{teamId}";
        public static string Invitation(Guid teamId, Guid invitationId) => $"/api/teaminvitations/{teamId}/{invitationId}";
    }
}
