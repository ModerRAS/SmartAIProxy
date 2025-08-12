# SmartAIProxy .NET 9 Implementation

[![.NET 9](https://img.shields.io/badge/.NET-9.0-blue)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![Build Status](https://github.com/ModerRAS/SmartAIProxy/workflows/CI/CD%20Pipeline/badge.svg)](https://github.com/ModerRAS/SmartAIProxy/actions)
[![License](https://img.shields.io/github/license/ModerRAS/SmartAIProxy)](LICENSE)

This is a complete .NET 9 implementation of the SmartAIProxy, a high-performance, extensible AI API gateway service that forwards API requests to major AI models (OpenAI, Anthropic Claude, Google Gemini, etc.). 

[中文文档](README_zh.md) 

## Features

- **API Gateway**: Forwards requests to AI service providers with intelligent routing
- **Intelligent Routing**: Selects optimal channels based on rules, quotas, and pricing
- **Configuration Management**: YAML-based configuration with hot reload
- **Channel Management**: Support for multiple AI providers with quota tracking
- **Rule Engine**: Expression-based routing rules using NCalc
- **Admin API**: RESTful endpoints for managing channels and rules
- **Security**: JWT authentication for admin endpoints, API key authentication for gateway
- **Monitoring**: Prometheus metrics integration
- **Fault Tolerance**: Retry mechanisms and circuit breaker patterns
- **Rate Limiting**: Per-API key rate limiting
- **Logging**: Structured logging with Serilog

## Architecture

The application follows a clean architecture pattern with the following layers:

1. **API Layer**: ASP.NET Core controllers and middleware
2. **Core Layer**: Business logic services (Configuration, Channels, Rules)
3. **Models Layer**: Data transfer objects and configuration models
4. **Middleware Layer**: Custom proxy middleware for request forwarding

## Getting Started

### Prerequisites

- .NET 9 SDK
- Docker (optional, for containerized deployment)

### Building the Application

```bash
cd SmartAIProxy
dotnet build
```

### Running the Application

```bash
cd SmartAIProxy
dotnet run
```

The application will start on port 8080 by default.

### Running Tests

```bash
cd ../SmartAIProxy.Tests
dotnet test
```

## Configuration

The application uses YAML configuration files located in the `config` directory. A default configuration file is created on first run.

### Key Configuration Sections

- **server**: Server settings (listen address, timeout, max connections)
- **channels**: AI service provider configurations
- **rules**: Routing rules with expressions
- **monitor**: Prometheus monitoring settings
- **security**: Authentication and rate limiting settings

## API Endpoints

### Gateway API

- `POST /v1/chat/completions` - Forward chat completion requests
- `POST /v1/completions` - Forward completion requests
- `POST /v1/embeddings` - Forward embedding requests
- `GET /healthz` - Health check endpoint

### Admin API

- `GET /api/channels` - Get all configured channels
- `POST /api/channels` - Add or update channel configuration
- `GET /api/rules` - Get all routing rules
- `POST /api/rules` - Add or update routing rule
- `GET /api/config` - Get current configuration
- `GET /api/health` - Admin API health check
- `POST /api/auth/login` - Login to get JWT token

## Docker Support

The application includes Docker configuration for containerized deployment:

```bash
docker build -t smartaiproxy .
docker run -p 8080:8080 smartaiproxy
```

## Monitoring

Prometheus metrics are exposed at `/metrics` endpoint. The application includes built-in metrics for:
- Request counts and response times
- Channel usage and success rates
- System health and resource usage

## Testing

The project includes comprehensive unit tests covering:
- Rule engine evaluation
- Channel service operations
- Configuration management

To run tests:
```bash
dotnet test
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is licensed under the MIT License.