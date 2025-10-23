# Use the official .NET 9 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy csproj files and restore dependencies first (for better caching)
COPY *.csproj ./
COPY Src/Shared/*.csproj ./Src/Shared/
RUN dotnet restore

# Copy all source code
COPY . ./
RUN dotnet publish -c Release -o out --no-restore

# Use the official .NET 9 runtime image for the final image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Create a non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Install SQLite (in case we need sqlite3 command line tool)
RUN apt-get update && apt-get install -y \
    sqlite3 \
    curl \
    && rm -rf /var/lib/apt/lists/* \
    && apt-get clean

# Copy the published app
COPY --from=build /app/out .

# Create directory for database and set permissions
RUN mkdir -p /app/data && chmod 777 /app/data

# For now, run as root to avoid permission issues
# USER appuser

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Add health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Run the application
ENTRYPOINT ["dotnet", "NotificationsService.dll"]