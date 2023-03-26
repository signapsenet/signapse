using HtmlAgilityPack;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Signapse.Server.Extensions
{
    public class OpenAuthHttpClient : IDisposable
    {
        private readonly Uri callbackUri;
        private readonly Uri remoteServer;
        private string bearerToken;
        private HttpClient httpClient = new HttpClient();

        public OpenAuthHttpClient(Uri callbackUri, Uri remoteServer, string bearerToken)
        {
            CreateClient();

            this.callbackUri = callbackUri;
            this.remoteServer = remoteServer;
            this.bearerToken = bearerToken;
        }

        public void Dispose()
        {
            this.httpClient.Dispose();
        }

        private void CreateClient()
        {
            this.httpClient?.Dispose();

            this.httpClient = new HttpClient(new SocketsHttpHandler()
            {
                UseCookies = true
            });
        }

        public async Task<bool> Authorize()
        {
            var authRes = await SendAuthorize();

            return authRes.IsSuccessStatusCode
                && authRes.StatusCode != System.Net.HttpStatusCode.Redirect;
        }

        private async Task<HttpResponseMessage> SendAuthorize()
        {
            var url = $"http://{remoteServer.Host}:{remoteServer.Port}/oauth/authorize";

            url = QueryHelpers.AddQueryString(url, new Dictionary<string, string>()
            {
                { "response_type", "code" },
                { "client_id", "29352735982374239857" },
                { "redirect_uri", $"http://{callbackUri.Host}:{callbackUri.Port}/oauth/callback" },
                { "scope", "create delete" },
                { "state", "xcoivjuywkdkhvusuye3kch" },
                { "code_challenge", "" },
                { "code_challenge_method", "S256" }
            });
            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            return await httpClient.SendAsync(request);
        }

        private async Task<HttpResponseMessage> SendLogin(string action, Dictionary<string, string> values)
        {
            var url = $"http://{remoteServer.Host}:{remoteServer.Port}{action}";

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new FormUrlEncodedContent(values);

            return await httpClient.SendAsync(request);
        }

        private Dictionary<string, string> ParseLoginForm(string html, string email, string password, out string action)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            action = string.Empty;
            if (doc.DocumentNode.SelectSingleNode("//form") is HtmlAgilityPack.HtmlNode formNode)
            {
                action = formNode.GetAttributeValue("action", string.Empty);
                var values = formNode.SelectNodes("descendant::input")
                    .Select(n => new
                    {
                        name = n.GetAttributeValue("name", string.Empty),
                        value = n.GetAttributeValue("value", string.Empty)
                    })
                    .Where(o => !string.IsNullOrWhiteSpace(o.name))
                    .ToDictionary(o => o.name, o => o.value);

                values["email"] = email;
                values["password"] = password;

                return values;
            }

            return new Dictionary<string, string>();
        }

        public async Task<bool> Login(string email, string password)
        {
            this.CreateClient();

            var authRes = await SendAuthorize();
            if (!authRes.IsSuccessStatusCode) return false;

            var html = await authRes.Content.ReadAsStringAsync();
            var values = ParseLoginForm(html, email, password, out var action);

            authRes = await SendLogin(action, values);

            return true;
        }

        public async Task<T?> Fetch<T>(HttpMethod method, string url, object? args = null)
        {
            T? res = Activator.CreateInstance<T>();

            await Task.CompletedTask;

            return res;
        }
    }
}
