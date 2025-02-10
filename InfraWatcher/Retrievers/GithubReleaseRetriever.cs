using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using InfraWatcher.Helpers;
using Json.Path;

namespace InfraWatcher.Retrievers;

public class GithubReleaseRetriever : ILinesRetriever
{
    public required string Repo { get; set; }
    public string Suffix { get; set; } = "latest";
    public string Path { get; set; } = "$.tag_name";
    
    public async Task<string[]> GetLines(IDictionary<string, string>? variables)
    {
        if (Repo.IsNullOrEmpty())
            throw new InvalidOperationException("Repo name is required");
        
        var path = JsonPath.Parse(Path);
        string url = $"https://api.github.com/repos/{Repo}/releases/{Suffix}";
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.Add("User-Agent", "curl/8.7.1");
        var response = await client.SendAsync(request);
        if (response is not { IsSuccessStatusCode: true })
            throw new InvalidOperationException($"HTTP status code {response.StatusCode}");

        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        var result = path.Evaluate(json).Matches.Select(x => x.Value?.ToString()).Where(x => x != null).ToArray();

        return result!;
    }
}