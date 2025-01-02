using System.Net;
using InfraWatcher.Helpers;
using WebDav;
using YamlDotNet.Serialization;

namespace InfraWatcher.Retrievers;

public class WebDavFileListRetriever : ILinesRetriever
{
    [YamlMember(Alias = "webdav")]
    public required string WebdavPath { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    
    public async Task<string[]> GetLines(IDictionary<string, string?>? variables)
    {
        if (Username.IsNullOrEmpty() != Password.IsNullOrEmpty())
        {
            throw new InvalidOperationException("Bot username and password must be specified");
        }
        
        var pars = new WebDavClientParams();
        if (!Username.IsNullOrEmpty())
        {
            pars.Credentials = new NetworkCredential(Username, Password);
        }
        var client = new WebDavClient(pars);
        var result = await client.Propfind(WebdavPath);

        return result.Resources.Where(x => !x.IsCollection).Select(x => x.Uri).ToArray();
    }
}