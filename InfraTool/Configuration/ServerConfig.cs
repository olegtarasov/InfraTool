namespace InfraTool.Configuration;

public class ServerConfig
{
    public int Port { get; set; } = 5015;
    public LokiConfig? Loki { get; set; }
}