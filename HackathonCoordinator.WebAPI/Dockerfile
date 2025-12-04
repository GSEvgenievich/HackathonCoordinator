FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 1. Копируем ТОЛЬКО файлы WebAPI проекта
COPY HackathonCoordinator.WebAPI/HackathonCoordinator.WebAPI.csproj .
RUN dotnet restore HackathonCoordinator.WebAPI.csproj

# 2. Копируем исходники WebAPI
COPY HackathonCoordinator.WebAPI/ .

# 3. Публикуем
RUN dotnet publish HackathonCoordinator.WebAPI.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "HackathonCoordinator.WebAPI.dll"]