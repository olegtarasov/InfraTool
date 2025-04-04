using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using InfraTool.Helpers;
using Json.Path;

namespace InfraTool.Retrievers;

public class GithubTagRetriever : ILinesRetriever
{
    public required string Repo { get; set; }
    public string Path { get; set; } = "$[0].name";
    
    public async Task<string[]> GetLines(IDictionary<string, string>? variables)
    {
        if (Repo.IsNullOrEmpty())
            throw new InvalidOperationException("Repo name is required");
        
        var path = JsonPath.Parse(Path);
        string url = $"https://api.github.com/repos/{Repo}/tags";
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.Add("User-Agent", "olegtarasov");
        var response = await client.SendAsync(request);
        if (response is not { IsSuccessStatusCode: true })
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"HTTP status code {response.StatusCode}. Response:\n{errorContent}");
        }

        //string content = await response.Content.ReadAsStringAsync(); 
        var json = await response.Content.ReadFromJsonAsync<JsonArray>();
        var result = path.Evaluate(json).Matches.Select(x => x.Value?.ToString()).Where(x => x != null).ToArray();

        return result!;
    }
}