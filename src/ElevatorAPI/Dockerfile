# ── Build stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution + all project files first for layer-cached restore
COPY ElevatorSystem.sln ./
COPY src/ElevatorAPI/ElevatorAPI.csproj                         src/ElevatorAPI/
COPY src/ElevatorSystem/ElevatorSystem.csproj                   src/ElevatorSystem/
COPY src/ElevatorPanel/ElevatorPanel.csproj                     src/ElevatorPanel/
COPY tests/ElevatorSystem.Tests/ElevatorSystem.Tests.csproj     tests/ElevatorSystem.Tests/
COPY tests/ElevatorAPI.Tests/ElevatorAPI.Tests.csproj           tests/ElevatorAPI.Tests/

RUN dotnet restore ElevatorSystem.sln

# Copy remaining source
COPY src/ElevatorAPI/    src/ElevatorAPI/
COPY src/ElevatorSystem/ src/ElevatorSystem/

RUN dotnet publish src/ElevatorAPI/ElevatorAPI.csproj \
    -c Release \
    -o /app/publish

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ElevatorAPI.dll"]
