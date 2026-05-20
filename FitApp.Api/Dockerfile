# ===== Build stage =====
# Используем полный SDK для restore + publish
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Копируем csproj отдельно, чтобы кешировать restore
COPY FitApp.Api.csproj ./
RUN dotnet restore FitApp.Api.csproj

# Копируем остальной код и публикуем
COPY . .
RUN dotnet publish FitApp.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# ===== Runtime stage =====
# Минимальный runtime без SDK
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Папка для persistent disk (на Render туда монтируется том)
RUN mkdir -p /data

COPY --from=build /app/publish .

# Render передаёт порт через переменную $PORT.
# Мы пробрасываем её в ASPNETCORE_URLS при запуске.
# Локально (docker run -p 8080:8080) порт будет 8080.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "FitApp.Api.dll"]
