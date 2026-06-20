FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY services/shared/ services/shared/
COPY services/document-processing/ services/document-processing/
RUN dotnet restore services/document-processing/UnifiedRewards.DocumentProcessing/UnifiedRewards.DocumentProcessing.csproj
RUN dotnet publish services/document-processing/UnifiedRewards.DocumentProcessing/UnifiedRewards.DocumentProcessing.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "UnifiedRewards.DocumentProcessing.dll"]
