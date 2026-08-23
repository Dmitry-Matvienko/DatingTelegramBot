# 1. Base Runtime Image (.NET 9 ASP.NET)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

# 2. SDK Build Image (.NET 9 SDK)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Копируем файлы проектов для кэширования слоев NuGet
COPY ["DatingBot.sln", "./"]
COPY ["src/DatingBot.Domain/DatingBot.Domain.csproj", "src/DatingBot.Domain/"]
COPY ["src/DatingBot.Application/DatingBot.Application.csproj", "src/DatingBot.Application/"]
COPY ["src/DatingBot.Infrastructure/DatingBot.Infrastructure.csproj", "src/DatingBot.Infrastructure/"]
COPY ["src/DatingBot.Bot/DatingBot.Bot.csproj", "src/DatingBot.Bot/"]
COPY ["tests/DatingBot.UnitTests/DatingBot.UnitTests.csproj", "tests/DatingBot.UnitTests/"]
COPY ["tests/DatingBot.IntegrationTests/DatingBot.IntegrationTests.csproj", "tests/DatingBot.IntegrationTests/"]

# Восстанавливаем зависимости
RUN dotnet restore "src/DatingBot.Bot/DatingBot.Bot.csproj"

# Копируем исходный код
COPY src/ src/

# Сборка и публикация
WORKDIR "/src/src/DatingBot.Bot"
RUN dotnet publish "DatingBot.Bot.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 3. Final Production Container
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "DatingBot.Bot.dll"]
