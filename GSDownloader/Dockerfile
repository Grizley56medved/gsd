# ИСПРАВЛЕНО: используем aspnet вместо runtime для поддержки веб-сервера
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
# Порт по умолчанию для .NET 8/9 внутри контейнера
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["GSDownloader/GSDownloader.csproj", "GSDownloader/"]
RUN dotnet restore "GSDownloader/GSDownloader.csproj"
COPY . .
WORKDIR "/src/GSDownloader"
RUN dotnet build "./GSDownloader.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./GSDownloader.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GSDownloader.dll"]