FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY services/shared/ services/shared/
COPY services/reimbursement-workflow/ services/reimbursement-workflow/
RUN dotnet restore services/reimbursement-workflow/UnifiedRewards.ReimbursementWorkflow/UnifiedRewards.ReimbursementWorkflow.csproj
RUN dotnet publish services/reimbursement-workflow/UnifiedRewards.ReimbursementWorkflow/UnifiedRewards.ReimbursementWorkflow.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "UnifiedRewards.ReimbursementWorkflow.dll"]
