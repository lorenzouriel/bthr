# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["bthr.Api/bthr.Api.csproj", "bthr.Api/"]
RUN dotnet restore "bthr.Api/bthr.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/bthr.Api"
RUN dotnet build "bthr.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "bthr.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Create non-root user for security
RUN groupadd -r finapi && useradd -r -g finapi finapi

# Copy published app
COPY --from=publish /app/publish .

# Set ownership
RUN chown -R finapi:finapi /app

# Switch to non-root user
USER finapi

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Set entry point
ENTRYPOINT ["dotnet", "bthr.Api.dll"]