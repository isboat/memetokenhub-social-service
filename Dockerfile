FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY . .
RUN dotnet restore MemeTokenHub.SocialService.sln && dotnet publish src/MemeTokenHub.SocialService.Api/MemeTokenHub.SocialService.Api.csproj -c Release -o /app --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "MemeTokenHub.SocialService.Api.dll"]
