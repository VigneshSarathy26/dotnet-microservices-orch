FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /App

# Copy solution and project files
COPY dotnet-microservices-orch.sln ./
COPY src/Orchestrator.Worker/Orchestrator.Worker.csproj src/Orchestrator.Worker/
COPY src/Orchestrator.StateMachines/Orchestrator.StateMachines.csproj src/Orchestrator.StateMachines/
COPY src/Orchestrator.Contracts/Orchestrator.Contracts.csproj src/Orchestrator.Contracts/
COPY src/Orchestrator.Infrastructure/Orchestrator.Infrastructure.csproj src/Orchestrator.Infrastructure/
COPY tests/Orchestrator.UnitTests/Orchestrator.UnitTests.csproj tests/Orchestrator.UnitTests/
COPY tests/Orchestrator.IntegrationTests/Orchestrator.IntegrationTests.csproj tests/Orchestrator.IntegrationTests/

# Restore the application
RUN dotnet restore

# Copy all source code and build the release package
COPY . ./
RUN dotnet publish src/Orchestrator.Worker/Orchestrator.Worker.csproj -c Release -o out

# Build the final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /App
COPY --from=build-env /App/out .
ENTRYPOINT ["dotnet", "Orchestrator.Worker.dll"]
