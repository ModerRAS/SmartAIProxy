// Package logger 提供统一日志采集，结构化、分级输出，文件轮转，接口与主流程兼容
package logger

import (
	"go.uber.org/zap"
	"go.uber.org/zap/zapcore"
	"gopkg.in/natefinch/lumberjack.v2"
	"SmartAIProxy/internal/config"
)

var (
	Logger *zap.Logger
)

// InitLogger 初始化日志系统，支持结构化、分级、文件轮转
func InitLogger(cfg config.LogConfig) {
	encoderCfg := zap.NewProductionEncoderConfig()
	encoderCfg.EncodeTime = zapcore.ISO8601TimeEncoder
	encoder := zapcore.NewJSONEncoder(encoderCfg)

	writer := zapcore.AddSync(&lumberjack.Logger{
		Filename:   cfg.File,
		MaxSize:    cfg.MaxSize,
		MaxBackups: cfg.MaxBackups,
		MaxAge:     cfg.MaxAge,
	})

	var level zapcore.Level
	switch cfg.Level {
	case "debug":
		level = zapcore.DebugLevel
	case "info":
		level = zapcore.InfoLevel
	case "warn":
		level = zapcore.WarnLevel
	case "error":
		level = zapcore.ErrorLevel
	default:
		level = zapcore.InfoLevel
	}

	core := zapcore.NewCore(encoder, writer, level)
	Logger = zap.New(core, zap.AddCaller(), zap.AddCallerSkip(1))
}

// Info 输出 info 级别日志
func Info(msg string, fields ...zapcore.Field) {
	if Logger != nil {
		Logger.Info(msg, fields...)
	}
}

// Error 输出 error 级别日志
func Error(msg string, fields ...zapcore.Field) {
	if Logger != nil {
		Logger.Error(msg, fields...)
	}
}

// Debug 输出 debug 级别日志
func Debug(msg string, fields ...zapcore.Field) {
	if Logger != nil {
		Logger.Debug(msg, fields...)
	}
}

// Sync 刷新日志缓冲
func Sync() {
	if Logger != nil {
		_ = Logger.Sync()
	}
}