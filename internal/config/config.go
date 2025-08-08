// 配置解析模块，仅限配置相关内容
package config

import (
	"fmt"
	"sync"

	"github.com/fsnotify/fsnotify"
	"github.com/spf13/viper"
)

// 配置结构体定义
type ServerConfig struct {
	Listen         string `mapstructure:"listen"`
	Timeout        int    `mapstructure:"timeout"`
	MaxConnections int    `mapstructure:"max_connections"`
	EnableHTTPS    bool   `mapstructure:"enable_https"`
	CertFile       string `mapstructure:"cert_file"`
	KeyFile        string `mapstructure:"key_file"`
}

type LogConfig struct {
	Level      string `mapstructure:"level"`
	File       string `mapstructure:"file"`
	MaxSize    int    `mapstructure:"max_size"`
	MaxBackups int    `mapstructure:"max_backups"`
	MaxAge     int    `mapstructure:"max_age"`
}

type AdminConfig struct {
	Enable   bool   `mapstructure:"enable"`
	Listen   string `mapstructure:"listen"`
	Username string `mapstructure:"username"`
	Password string `mapstructure:"password"`
}

type ChannelConfig struct {
	Name           string   `mapstructure:"name"`
	Type           string   `mapstructure:"type"`
	Endpoint       string   `mapstructure:"endpoint"`
	APIKey         string   `mapstructure:"api_key"`
	PricePerToken  float64  `mapstructure:"price_per_token"`
	DailyLimit     int      `mapstructure:"daily_limit"`
	HourLimit      int      `mapstructure:"hour_limit"`
	Priority       int      `mapstructure:"priority"`
	DiscountTime   []string `mapstructure:"discount_time"`
	DiscountPrice  float64  `mapstructure:"discount_price"`
}

type RuleConfig struct {
	Name       string `mapstructure:"name"`
	Channel    string `mapstructure:"channel"`
	Expression string `mapstructure:"expression"`
}

type ErrorHandlingConfig struct {
	CheckOutputComplete bool `mapstructure:"check_output_complete"`
	RetryOnIncomplete   int  `mapstructure:"retry_on_incomplete"`
	ReturnStandardError bool `mapstructure:"return_standard_error"`
}

type MonitorConfig struct {
	Enable           bool   `mapstructure:"enable"`
	PrometheusListen string `mapstructure:"prometheus_listen"`
}

// 总配置结构体
type Config struct {
	Server        ServerConfig        `mapstructure:"server"`
	Log           LogConfig           `mapstructure:"log"`
	Admin         AdminConfig         `mapstructure:"admin"`
	Channels      []ChannelConfig     `mapstructure:"channels"`
	Rules         []RuleConfig        `mapstructure:"rules"`
	ErrorHandling ErrorHandlingConfig `mapstructure:"error_handling"`
	Monitor       MonitorConfig       `mapstructure:"monitor"`
}

var (
	config     *Config
	configLock sync.RWMutex
)

// 加载配置
func LoadConfig(configPath string) (*Config, error) {
	v := viper.New()
	v.SetConfigFile(configPath)
	v.SetConfigType("yaml")
	v.AutomaticEnv()

	if err := v.ReadInConfig(); err != nil {
		return nil, fmt.Errorf("读取配置失败: %w", err)
	}

	var cfg Config
	if err := v.Unmarshal(&cfg); err != nil {
		return nil, fmt.Errorf("解析配置失败: %w", err)
	}

	configLock.Lock()
	config = &cfg
	configLock.Unlock()

	// 监听配置文件变更，自动热更新
	v.WatchConfig()
	v.OnConfigChange(func(e fsnotify.Event) {
		var newCfg Config
		if err := v.Unmarshal(&newCfg); err == nil {
			configLock.Lock()
			config = &newCfg
			configLock.Unlock()
			fmt.Printf("配置已热更新: %s\n", e.Name)
		} else {
			fmt.Printf("配置热更新失败: %v\n", err)
		}
	})

	return config, nil
}

// 获取当前配置（线程安全）
func GetConfig() *Config {
	configLock.RLock()
	defer configLock.RUnlock()
	return config
}