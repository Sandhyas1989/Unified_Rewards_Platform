FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY services/shared/ services/shared/
COPY services/employee-profile/ services/employee-profile/
RUN dotnet restore services/employee-profile/UnifiedRewards.EmployeeProfile/UnifiedRewards.EmployeeProfile.csproj
RUN dotnet publish services/employee-profile/UnifiedRewards.EmployeeProfile/UnifiedRewards.EmployeeProfile.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "UnifiedRewards.EmployeeProfile.dll"]
