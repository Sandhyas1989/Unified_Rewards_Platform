FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY services/shared/ services/shared/
COPY services/payroll-integration/ services/payroll-integration/
RUN dotnet restore services/payroll-integration/UnifiedRewards.PayrollIntegration/UnifiedRewards.PayrollIntegration.csproj
RUN dotnet publish services/payroll-integration/UnifiedRewards.PayrollIntegration/UnifiedRewards.PayrollIntegration.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "UnifiedRewards.PayrollIntegration.dll"]
