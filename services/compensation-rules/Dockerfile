FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY services/shared/ services/shared/
COPY services/compensation-rules/ services/compensation-rules/
RUN dotnet restore services/compensation-rules/UnifiedRewards.CompensationRules/UnifiedRewards.CompensationRules.csproj
RUN dotnet publish services/compensation-rules/UnifiedRewards.CompensationRules/UnifiedRewards.CompensationRules.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "UnifiedRewards.CompensationRules.dll"]
