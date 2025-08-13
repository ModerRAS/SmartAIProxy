using SmartAIProxy.Models.Config;
using System.Collections.Concurrent;
using SmartAIProxy.Core.Config;

namespace SmartAIProxy.Core.Channels;

/// <summary>
/// 通道服务接口，提供AI服务通道的管理功能
/// </summary>
public interface IChannelService
{
    /// <summary>
    /// 获取所有配置的AI服务通道
    /// </summary>
    /// <returns>通道配置列表</returns>
    List<ChannelConfig> GetChannels();

    /// <summary>
    /// 根据名称获取指定的AI服务通道
    /// </summary>
    /// <param name="name">通道名称</param>
    /// <returns>找到的通道配置，如果未找到则返回null</returns>
    ChannelConfig? GetChannelByName(string name);

    /// <summary>
    /// 添加或更新AI服务通道配置
    /// </summary>
    /// <param name="channel">通道配置信息</param>
    void AddOrUpdateChannel(ChannelConfig channel);

    /// <summary>
    /// 移除指定的AI服务通道
    /// </summary>
    /// <param name="name">要移除的通道名称</param>
    void RemoveChannel(string name);

    /// <summary>
    /// 更新AI服务通道的状态
    /// </summary>
    /// <param name="name">通道名称</param>
    /// <param name="status">新状态</param>
    void UpdateChannelStatus(string name, string status);

    /// <summary>
    /// 获取所有通道的使用情况统计
    /// </summary>
    /// <returns>通道使用情况字典，键为通道名称，值为使用的令牌数</returns>
    Dictionary<string, int> GetChannelUsage();

    /// <summary>
    /// 更新通道的使用情况
    /// </summary>
    /// <param name="channelName">通道名称</param>
    /// <param name="tokens">使用的令牌数</param>
    void UpdateChannelUsage(string channelName, int tokens);
}

/// <summary>
/// 通道服务实现类，负责管理AI服务通道的配置和使用情况
/// </summary>
public class ChannelService : IChannelService
{
    private readonly IConfigurationService _configService;
    private readonly ILogger<ChannelService> _logger;
    private readonly ConcurrentDictionary<string, int> _channelUsage;

    /// <summary>
    /// 通道服务构造函数
    /// </summary>
    /// <param name="configService">配置服务</param>
    /// <param name="logger">日志记录器</param>
    public ChannelService(IConfigurationService configService, ILogger<ChannelService> logger)
    {
        _configService = configService;
        _logger = logger;
        _channelUsage = new ConcurrentDictionary<string, int>();
    }

    /// <summary>
    /// 获取所有配置的AI服务通道
    /// </summary>
    /// <returns>通道配置列表</returns>
    public List<ChannelConfig> GetChannels()
    {
        var config = _configService.GetConfig();
        return config.Channels;
    }

    /// <summary>
    /// 根据名称获取指定的AI服务通道
    /// </summary>
    /// <param name="name">通道名称</param>
    /// <returns>找到的通道配置，如果未找到则返回null</returns>
    public ChannelConfig? GetChannelByName(string name)
    {
        var config = _configService.GetConfig();
        return config.Channels.FirstOrDefault(c => c.Name == name);
    }

    /// <summary>
    /// 添加或更新AI服务通道配置
    /// </summary>
    /// <param name="channel">通道配置信息</param>
    public void AddOrUpdateChannel(ChannelConfig channel)
    {
        var config = _configService.GetConfig();
        var existingChannel = config.Channels.FirstOrDefault(c => c.Name == channel.Name);
        
        if (existingChannel != null)
        {
            // 更新现有通道
            var index = config.Channels.IndexOf(existingChannel);
            config.Channels[index] = channel;
        }
        else
        {
            // 添加新通道
            config.Channels.Add(channel);
        }
        
        _configService.UpdateConfig(config);
        _logger.LogInformation("Channel '{ChannelName}' added/updated", channel.Name);
    }

    /// <summary>
    /// 移除指定的AI服务通道
    /// </summary>
    /// <param name="name">要移除的通道名称</param>
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

    /// <summary>
    /// 更新AI服务通道的状态
    /// </summary>
    /// <param name="name">通道名称</param>
    /// <param name="status">新状态</param>
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

    /// <summary>
    /// 获取所有通道的使用情况统计
    /// </summary>
    /// <returns>通道使用情况字典，键为通道名称，值为使用的令牌数</returns>
    public Dictionary<string, int> GetChannelUsage()
    {
        return new Dictionary<string, int>(_channelUsage);
    }

    /// <summary>
    /// 更新通道的使用情况
    /// </summary>
    /// <param name="channelName">通道名称</param>
    /// <param name="tokens">使用的令牌数</param>
    public void UpdateChannelUsage(string channelName, int tokens)
    {
        _channelUsage.AddOrUpdate(channelName, tokens, (key, oldValue) => oldValue + tokens);
    }
}