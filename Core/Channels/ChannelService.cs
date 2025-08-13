using SmartAIProxy.Models.Config;
using System.Collections.Concurrent;
using SmartAIProxy.Core.Config;

namespace SmartAIProxy.Core.Channels;

public interface IChannelService
{
    List<ChannelConfig> GetChannels();
    ChannelConfig? GetChannelByName(string name);
    void AddOrUpdateChannel(ChannelConfig channel);
    void RemoveChannel(string name);
    void UpdateChannelStatus(string name, string status);
    Dictionary<string, int> GetChannelUsage();
    void UpdateChannelUsage(string channelName, int tokens);
}

public class ChannelService : IChannelService
{
    private readonly IConfigurationService _configService;
    private readonly ILogger<ChannelService> _logger;
    private readonly ConcurrentDictionary<string, int> _channelUsage;

    public ChannelService(IConfigurationService configService, ILogger<ChannelService> logger)
    {
        _configService = configService;
        _logger = logger;
        _channelUsage = new ConcurrentDictionary<string, int>();
    }

    public List<ChannelConfig> GetChannels()
    {
        var config = _configService.GetConfig();
        return config.Channels;
    }

    public ChannelConfig? GetChannelByName(string name)
    {
        var config = _configService.GetConfig();
        return config.Channels.FirstOrDefault(c => c.Name == name);
    }

    public void AddOrUpdateChannel(ChannelConfig channel)
    {
        var config = _configService.GetConfig();
        var existingChannel = config.Channels.FirstOrDefault(c => c.Name == channel.Name);
        
        if (existingChannel != null)
        {
            // Update existing channel
            var index = config.Channels.IndexOf(existingChannel);
            config.Channels[index] = channel;
        }
        else
        {
            // Add new channel
            config.Channels.Add(channel);
        }
        
        _configService.UpdateConfig(config);
        _logger.LogInformation("Channel '{ChannelName}' added/updated", channel.Name);
    }

    public void RemoveChannel(string name)
    {
        var config = _configService.GetConfig();
        var channel = config.Channels.FirstOrDefault(c => c.Name == name);
        
        if (channel != null)
        {
            config.Channels.Remove(channel);
            _configService.UpdateConfig(config);
            _logger.LogInformation("Channel '{ChannelName}' removed", name);
        }
    }

    public void UpdateChannelStatus(string name, string status)
    {
        var config = _configService.GetConfig();
        var channel = config.Channels.FirstOrDefault(c => c.Name == name);
        
        if (channel != null)
        {
            channel.Status = status;
            _configService.UpdateConfig(config);
            _logger.LogInformation("Channel '{ChannelName}' status updated to '{Status}'", name, status);
        }
    }

    public Dictionary<string, int> GetChannelUsage()
    {
        return new Dictionary<string, int>(_channelUsage);
    }

    public void UpdateChannelUsage(string channelName, int tokens)
    {
        _channelUsage.AddOrUpdate(channelName, tokens, (key, oldValue) => oldValue + tokens);
    }
}