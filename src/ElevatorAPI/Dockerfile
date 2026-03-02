# ── Build stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution + project files first for layer-cached restore
COPY ElevatorSystem.sln ./
COPY src/ElevatorAPI/ElevatorAPI.csproj         src/ElevatorAPI/
COPY src/ElevatorSystem/ElevatorSystem.csproj   src/ElevatorSystem/

RUN dotnet restore src/ElevatorAPI/ElevatorAPI.csproj

# Copy remaining source
COPY src/ElevatorAPI/    src/ElevatorAPI/
COPY src/ElevatorSystem/ src/ElevatorSystem/

RUN dotnet publish src/ElevatorAPI/ElevatorAPI.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ElevatorAPI.dll"]
