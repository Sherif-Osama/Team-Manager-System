using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TeamManager.IntegrationTest.Common
{
    public static class ValidationAssertions
    {
        public static async Task AssertFieldErrorAsync(HttpResponseMessage response, string fieldName)
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.True(body.TryGetProperty("errors", out var errors));
            Assert.Contains(errors.EnumerateObject(), property => property.Name.EndsWith(fieldName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
