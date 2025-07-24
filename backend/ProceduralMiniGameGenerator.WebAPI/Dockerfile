# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["backend/ProceduralMiniGameGenerator.WebAPI/ProceduralMiniGameGenerator.WebAPI.csproj", "backend/ProceduralMiniGameGenerator.WebAPI/"]
COPY ["src/ProceduralMiniGameGenerator.csproj", "src/"]

# Restore dependencies
RUN dotnet restore "backend/ProceduralMiniGameGenerator.WebAPI/ProceduralMiniGameGenerator.WebAPI.csproj"

# Copy source code
COPY . .

# Build the application
WORKDIR "/src/backend/ProceduralMiniGameGenerator.WebAPI"
RUN dotnet build "ProceduralMiniGameGenerator.WebAPI.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "ProceduralMiniGameGenerator.WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create logs directory
RUN mkdir -p /app/logs

EXPOSE 80
EXPOSE 443

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProceduralMiniGameGenerator.WebAPI.dll"]