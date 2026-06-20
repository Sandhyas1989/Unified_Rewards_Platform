FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY services/shared/ services/shared/
COPY services/reporting-compliance/ services/reporting-compliance/
RUN dotnet restore services/reporting-compliance/UnifiedRewards.ReportingCompliance/UnifiedRewards.ReportingCompliance.csproj
RUN dotnet publish services/reporting-compliance/UnifiedRewards.ReportingCompliance/UnifiedRewards.ReportingCompliance.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "UnifiedRewards.ReportingCompliance.dll"]
