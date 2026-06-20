FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY services/shared/ services/shared/
COPY services/benefits-catalogue/ services/benefits-catalogue/
RUN dotnet restore services/benefits-catalogue/UnifiedRewards.BenefitsCatalogue/UnifiedRewards.BenefitsCatalogue.csproj
RUN dotnet publish services/benefits-catalogue/UnifiedRewards.BenefitsCatalogue/UnifiedRewards.BenefitsCatalogue.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "UnifiedRewards.BenefitsCatalogue.dll"]
