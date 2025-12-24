# Deployment Guide

## PostgreSQL Connection Configuration

The application reads the PostgreSQL connection string from configuration in the following priority order:

1. **Environment Variables** (highest priority - recommended for production)
2. `appsettings.Production.json`
3. `appsettings.Development.json`
4. `appsettings.json`

## DigitalOcean Deployment

### Option 1: DigitalOcean Managed Databases (Recommended)

1. **Create a PostgreSQL database** in DigitalOcean:
   - Go to Databases → Create Database Cluster
   - Choose PostgreSQL
   - Select your region and plan
   - Note the connection details (host, port, database, user, password)

2. **Set Environment Variables** in your DigitalOcean App Platform:
   - Go to your App → Settings → Environment Variables
   - Add: `ConnectionStrings__DefaultConnection`
   - Value format:
     ```
     Host=your-db-host.db.ondigitalocean.com;Port=25060;Database=defaultdb;Username=doadmin;Password=your-password;SslMode=Require;
     ```
   
   **Important**: Use double underscore `__` in the environment variable name. This maps to the nested JSON structure `ConnectionStrings:DefaultConnection`.

3. **For connection pooling** (if using a connection pooler):
   ```
   Host=your-db-host.db.ondigitalocean.com;Port=25060;Database=defaultdb;Username=doadmin;Password=your-password;SslMode=Require;Pooling=true;MinPoolSize=0;MaxPoolSize=100;
   ```

### Option 2: Self-Managed PostgreSQL Droplet

1. Create a PostgreSQL droplet or install PostgreSQL on an existing droplet
2. Configure firewall rules to allow connections from your app droplet
3. Set the environment variable as above with your droplet's IP/hostname

### Option 3: External PostgreSQL Service

If using an external PostgreSQL service (AWS RDS, Azure Database, etc.), use the same environment variable approach with your provider's connection string format.

## Connection String Format

The PostgreSQL connection string should follow this format:

```
Host=hostname;Port=5432;Database=dbname;Username=user;Password=password;SslMode=Require;
```

### Required Parameters:
- `Host` - Database server hostname or IP
- `Port` - Database port (default: 5432, DigitalOcean Managed: usually 25060)
- `Database` - Database name
- `Username` - Database username
- `Password` - Database password

### Optional but Recommended for Production:
- `SslMode=Require` - Enforce SSL connections (required for DigitalOcean Managed Databases)
- `Pooling=true` - Enable connection pooling
- `MinPoolSize=0` - Minimum connections in pool
- `MaxPoolSize=100` - Maximum connections in pool

## Security Best Practices

1. **Never commit connection strings with passwords to version control**
2. **Always use environment variables for production credentials**
3. **Use SSL/TLS connections in production** (SslMode=Require)
4. **Restrict database access** to only your app's IP addresses when possible
5. **Use separate databases** for development, staging, and production
6. **Rotate passwords regularly**

## Local Development

For local development, the connection string is set in `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=GifCampDb;Username=postgres;Password=postgres"
  }
}
```

## Testing the Connection

After deploying, verify the connection by:
1. Checking application logs for database connection errors
2. Testing the `/login` endpoint
3. Verifying the database schema was created (run migrations if needed)

## Running Migrations in Production

If you need to run migrations on DigitalOcean:

1. SSH into your app instance (if using Droplets)
2. Or use DigitalOcean's console/shell feature
3. Navigate to your app directory
4. Run: `dotnet ef database update`

Alternatively, you can run migrations automatically on app startup by adding migration code to `Program.cs` (not recommended for high-availability scenarios).

