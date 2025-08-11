FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["SmartAIProxy.csproj", "."]
RUN dotnet restore "./SmartAIProxy.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "SmartAIProxy.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SmartAIProxy.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN mkdir -p /app/logs /app/config
RUN chown -R www-data:www-data /app/logs /app/config
USER www-data
ENTRYPOINT ["dotnet", "SmartAIProxy.dll"]