using SmartAIProxy.Core.Channels;
using SmartAIProxy.Core.Config;
using SmartAIProxy.Models.Config;
using Xunit;
using Microsoft.Extensions.Logging;
using Moq;

namespace SmartAIProxy.Tests;

public class ChannelServiceTests
{
    private readonly Mock<IConfigurationService> _mockConfigService;
    private readonly Mock<ILogger<ChannelService>> _mockLogger;
    private readonly ChannelService _channelService;

    public ChannelServiceTests()
    {
        _mockConfigService = new Mock<IConfigurationService>();
        _mockLogger = new Mock<ILogger<ChannelService>>();
        _channelService = new ChannelService(_mockConfigService.Object, _mockLogger.Object);
    }

    [Fact]
    public void GetChannels_ReturnsAllChannels()
    {
        // Arrange
        var config = new AppConfig
        {
            Channels = new List<ChannelConfig>
            {
                new ChannelConfig { Name = "Channel 1" },
                new ChannelConfig { Name = "Channel 2" }
            }
        };

        _mockConfigService.Setup(x => x.GetConfig()).Returns(config);

        // Act
        var result = _channelService.GetChannels();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Channel 1", result[0].Name);
        Assert.Equal("Channel 2", result[1].Name);
    }

    [Fact]
    public void GetChannelByName_ReturnsCorrectChannel()
    {
        // Arrange
        var config = new AppConfig
        {
            Channels = new List<ChannelConfig>
            {
                new ChannelConfig { Name = "Test Channel" },
                new ChannelConfig { Name = "Another Channel" }
            }
        };

        _mockConfigService.Setup(x => x.GetConfig()).Returns(config);

        // Act
        var result = _channelService.GetChannelByName("Test Channel");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Channel", result.Name);
    }

    [Fact]
    public void GetChannelByName_WhenNotFound_ReturnsNull()
    {
        // Arrange
        var config = new AppConfig
        {
            Channels = new List<ChannelConfig>
            {
                new ChannelConfig { Name = "Test Channel" }
            }
        };

        _mockConfigService.Setup(x => x.GetConfig()).Returns(config);

        // Act
        var result = _channelService.GetChannelByName("Nonexistent Channel");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void AddOrUpdateChannel_AddsNewChannel()
    {
        // Arrange
        var config = new AppConfig
        {
            Channels = new List<ChannelConfig>()
        };

        var newChannel = new ChannelConfig
        {
            Name = "New Channel",
            Type = "openai"
        };

        _mockConfigService.Setup(x => x.GetConfig()).Returns(config);

        // Act
        _channelService.AddOrUpdateChannel(newChannel);

        // Assert
        Assert.Single(config.Channels);
        Assert.Equal("New Channel", config.Channels[0].Name);
        _mockConfigService.Verify(x => x.UpdateConfig(config), Times.Once);
    }

    [Fact]
    public void AddOrUpdateChannel_UpdatesExistingChannel()
    {
        // Arrange
        var existingChannel = new ChannelConfig
        {
            Name = "Existing Channel",
            Type = "openai",
            Endpoint = "https://api.openai.com/v1"
        };

        var config = new AppConfig
        {
            Channels = new List<ChannelConfig> { existingChannel }
        };

        var updatedChannel = new ChannelConfig
        {
            Name = "Existing Channel",
            Type = "claude",
            Endpoint = "https://api.anthropic.com/v1"
        };

        _mockConfigService.Setup(x => x.GetConfig()).Returns(config);

        // Act
        _channelService.AddOrUpdateChannel(updatedChannel);

        // Assert
        Assert.Single(config.Channels);
        Assert.Equal("claude", config.Channels[0].Type);
        Assert.Equal("https://api.anthropic.com/v1", config.Channels[0].Endpoint);
        _mockConfigService.Verify(x => x.UpdateConfig(config), Times.Once);
    }

    [Fact]
    public void UpdateChannelUsage_TracksUsage()
    {
        // Act
        _channelService.UpdateChannelUsage("Test Channel", 100);
        _channelService.UpdateChannelUsage("Test Channel", 50);
        var usage = _channelService.GetChannelUsage();

        // Assert
        Assert.True(usage.ContainsKey("Test Channel"));
        Assert.Equal(150, usage["Test Channel"]);
    }
}