# syntax=docker/dockerfile:1

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["ProceduralMiniGameGenerator.WebAPI/ProceduralMiniGameGenerator.WebAPI.csproj", "ProceduralMiniGameGenerator.WebAPI/"]
RUN dotnet restore "ProceduralMiniGameGenerator.WebAPI/ProceduralMiniGameGenerator.WebAPI.csproj"

# Copy source code and build
COPY . .
WORKDIR "/src/ProceduralMiniGameGenerator.WebAPI"
RUN dotnet build "ProceduralMiniGameGenerator.WebAPI.csproj" -c Release -o /app/build

# Test stage
FROM build AS test
WORKDIR /src
RUN dotnet test "ProceduralMiniGameGenerator.WebAPI.Tests/ProceduralMiniGameGenerator.WebAPI.Tests.csproj" -c Release --logger trx --results-directory /testresults

# Publish stage
FROM build AS publish
RUN dotnet publish "ProceduralMiniGameGenerator.WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

# Create non-root user
RUN addgroup -g 1001 -S appgroup && \
    adduser -S appuser -u 1001 -G appgroup

# Copy published app
COPY --from=publish /app/publish .
RUN chown -R appuser:appgroup /app

# Switch to non-root user
USER appuser

EXPOSE 8080
ENTRYPOINT ["dotnet", "ProceduralMiniGameGenerator.WebAPI.dll"]