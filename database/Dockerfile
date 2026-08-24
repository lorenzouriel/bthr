# Flyway Docker Setup for SQL Server Migrations
FROM flyway/flyway:latest

# Copy migration files
COPY migrations /flyway/sql

# Copy configuration
COPY flyway.toml /flyway/conf/flyway.toml

# Set working directory
WORKDIR /flyway

# Default command (can be overridden)
# Usage: docker run --rm <image> migrate
CMD ["migrate"]
