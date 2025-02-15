# Use the official PostgreSQL image as the base
FROM postgres:16

# Install wal2json plugin using apt-get
RUN apt-get update && apt-get install -y postgresql-16-wal2json

# Set environment variables for the PostgreSQL instance
ENV POSTGRES_USER=postgres
ENV POSTGRES_DB=logical_replication
ENV POSTGRES_HOST_AUTH_METHOD=trust

# Add configuration files for logical replication
COPY ./postgresql.conf /etc/postgresql/postgresql.conf

# Ensure the custom configuration file is used
CMD ["postgres", "-c", "config_file=/etc/postgresql/postgresql.conf"]