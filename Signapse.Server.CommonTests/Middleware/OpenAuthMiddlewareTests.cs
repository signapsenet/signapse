using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signapse.Server.Tests;

namespace Signapse.Server.Middleware.Tests
{
    [TestClass()]
    public class OpenAuthMiddlewareTests
    {
        [TestClass]
        public class OAuthTests
        {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            private HttpClient _httpClient;
            private TestAffiliateServer _server;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

            [TestInitialize]
            public void TestInitialize()
            {
                _httpClient = new HttpClient();
                _server = new TestAffiliateServer();
            }

            [TestMethod]
            public async Task TestOAuthAuthenticationFlow()
            {
                // Make a request to a protected resource without an access token
                var response = await _httpClient.GetAsync("https://example.com/protected-resource");

                // Verify that the response contains a redirect to the authentication provider's login page
                Assert.AreEqual(System.Net.HttpStatusCode.Redirect, response.StatusCode);
                Assert.IsTrue(response.Headers.Location.ToString().Contains("https://example.com/oauth2/auth"));

                // Simulate the user logging in to the authentication provider and getting redirected back to the application
                var authCode = "abc123"; // Replace with a valid authorization code
                var redirectUri = "https://example.com/callback"; // Replace with the application's redirect URI
                response = await _httpClient.GetAsync($"https://example.com/oauth2/token?code={authCode}&redirect_uri={redirectUri}");

                // Verify that the response contains an access token and refresh token
                var content = await response.Content.ReadAsStringAsync();
                Assert.IsTrue(content.Contains("access_token"));
                Assert.IsTrue(content.Contains("refresh_token"));
            }

            [TestMethod]
            public async Task TestTokenExpirationAndRefresh()
            {
                // Make a request to a protected resource with a valid access token
                var accessToken = "xyz456"; // Replace with a valid access token
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var response = await _httpClient.GetAsync("https://example.com/protected-resource");

                // Verify that the response is successful
                Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);

                // Wait for the access token to expire
                await Task.Delay(TimeSpan.FromMinutes(10)); // Replace with the token's actual expiration time

                // Make another request to the protected resource with the same access token
                response = await _httpClient.GetAsync("https://example.com/protected-resource");

                // Verify that the response contains a 401 Unauthorized status code
                Assert.AreEqual(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);

                // Simulate refreshing the access token using the refresh token
                var refreshToken = "def789"; // Replace with a valid refresh token
                response = await _httpClient.GetAsync($"https://example.com/oauth2/token?grant_type=refresh_token&refresh_token={refreshToken}");

                // Verify that the response contains a new access token and refresh token
                var content = await response.Content.ReadAsStringAsync();
                Assert.IsTrue(content.Contains("access_token"));
                Assert.IsTrue(content.Contains("refresh_token"));
            }

            [TestMethod]
            public async Task TestInvalidAccessToken()
            {
                // Make a request to a protected resource with an invalid access token
                var accessToken = "invalid-token";
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var response = await _httpClient.GetAsync("https://example.com/protected-resource");

                // Verify that the response contains a 401 Unauthorized status code
                Assert.AreEqual(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
            }

            [TestMethod]
            public async Task TestRevokedAccessToken()
            {
                // Make a request to a protected resource with a revoked access token
                var accessToken = "revoked-token"; // Replace with a revoked access token
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var response = await _httpClient.GetAsync("https://example.com/protected-resource");

                // Verify that the response contains a 401 Unauthorized status code
                Assert.AreEqual(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
            }

            [TestMethod]
            public async Task TestInvalidRefreshToken()
            {
                // Attempt to refresh an access token with an invalid refresh token
                var refreshToken = "invalid-refresh-token";
                var response = await _httpClient.GetAsync($"https://example.com/oauth2/token?grant_type=refresh_token&refresh_token={refreshToken}");

                // Verify that the response contains a 400 Bad Request status code
                Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            }

            [TestMethod]
            public async Task TestExpiredRefreshToken()
            {
                // Attempt to refresh an access token with an expired refresh token
                var refreshToken = "expired-refresh-token"; // Replace with an expired refresh token
                var response = await _httpClient.GetAsync($"https://example.com/oauth2/token?grant_type=refresh_token&refresh_token={refreshToken}");

                // Verify that the response contains a 400 Bad Request status code
                Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            }

            [TestMethod]
            public async Task TestInvalidAuthorizationCode()
            {
                // Attempt to exchange an invalid authorization code for an access token
                var authCode = "invalid-code";
                var redirectUri = "https://example.com/callback";
                var response = await _httpClient.GetAsync($"https://example.com/oauth2/token?code={authCode}&redirect_uri={redirectUri}");

                // Verify that the response contains a 400 Bad Request status code
                Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            }

            [TestMethod]
            public async Task TestMissingRedirectUri()
            {
                // Attempt to exchange an authorization code for an access token without providing a redirect URI
                var authCode = "abc123";
                var response = await _httpClient.GetAsync($"https://example.com/oauth2/token?code={authCode}");

                // Verify that the response contains a 400 Bad Request status code
                Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            }
        }
    }
}