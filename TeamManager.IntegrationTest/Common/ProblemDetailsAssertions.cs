using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TeamManager.IntegrationTest.Common
{
    public static class ProblemDetailsAssertions
    {
        // A status code alone cannot tell two rejected reasons apart, so the detail text is checked as well.
        public static async Task AssertDetailAsync(HttpResponseMessage response, HttpStatusCode expectedStatus, string fragment)
        {
            Assert.Equal(expectedStatus, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Contains(fragment, body.GetProperty("detail").GetString(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
