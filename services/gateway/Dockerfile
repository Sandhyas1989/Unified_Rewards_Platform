FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY services/gateway/ services/gateway/
RUN dotnet restore services/gateway/UnifiedRewards.Gateway/UnifiedRewards.Gateway.csproj
RUN dotnet publish services/gateway/UnifiedRewards.Gateway/UnifiedRewards.Gateway.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "UnifiedRewards.Gateway.dll"]
