# Environment Variables Configuration

Create a `.env` file in the project root (same directory as `GifCampAPI.sln`) with the following variables:

```env
# Storage Provider Configuration
# Options: "local" or "digitalocean"
# Default: "local" (in development)
STORAGE_PROVIDER=local

# DigitalOcean Spaces Configuration (only needed if STORAGE_PROVIDER=digitalocean)
# Get these from your DigitalOcean Spaces control panel
DO_SPACES_ENDPOINT=https://nyc3.digitaloceanspaces.com
DO_SPACES_ACCESS_KEY=your_access_key_here
DO_SPACES_SECRET_KEY=your_secret_key_here
DO_SPACES_BUCKET=your_bucket_name_here
DO_SPACES_REGION=nyc3
DO_SPACES_CDN_URL=https://your-cdn-url.nyc3.cdn.digitaloceanspaces.com

# Database Connection (optional, can also use appsettings.json)
# ConnectionStrings__DefaultConnection=Host=localhost;Database=GifCampDb;Username=chris
```

## Storage Provider Options

### Local Storage (Default for Development)
- Set `STORAGE_PROVIDER=local` or leave unset
- Files are saved to `Content/{userId}/` directory in the project root
- No additional configuration needed

### DigitalOcean Spaces
- Set `STORAGE_PROVIDER=digitalocean`
- Requires all DO_SPACES_* environment variables to be set
- Files are uploaded to your DigitalOcean Spaces bucket
- If CDN URL is provided, public URLs will use the CDN endpoint

## Configuration Priority

The application reads configuration in this order (highest priority first):
1. Environment variables (from `.env` file or system environment)
2. `appsettings.Production.json` (for production environment)
3. `appsettings.Development.json` (for development environment)
4. `appsettings.json` (base configuration)

