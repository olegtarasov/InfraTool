namespace InfraTool.Configuration;

public class ServerConfig
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 5015;
    public LokiConfig? Loki { get; set; }
}