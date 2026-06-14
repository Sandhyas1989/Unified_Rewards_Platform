# Production container image for the Employee Profile Service.
# NOTE: This is authored as a deliverable; it is NOT built on the dev laptop (no local Docker).
# It is built by cloud CI (GitHub Actions) or in GitHub Codespaces / Azure Cloud Shell.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY UnifiedRewards.EmployeeProfile.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app ./
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "UnifiedRewards.EmployeeProfile.dll"]
