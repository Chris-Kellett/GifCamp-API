# Connecting to PostgreSQL Database in DBeaver

## Connection Details

Use these settings to connect to your local PostgreSQL database:

- **Host**: `localhost`
- **Port**: `5432`
- **Database**: `GifCampDb`
- **Username**: `chris`
- **Password**: *(leave empty - using trust authentication)*
- **SSL**: Disable (not needed for local connections)

## Step-by-Step Instructions

1. **Open DBeaver**
   - Launch DBeaver application

2. **Create New Database Connection**
   - Click the "New Database Connection" button (plug icon) in the toolbar
   - Or: Right-click on "Databases" → "New Database Connection"
   - Or: Database → New Database Connection (from menu)

3. **Select PostgreSQL**
   - In the connection wizard, search for "PostgreSQL"
   - Select "PostgreSQL" from the list
   - Click "Next"

4. **Configure Connection Settings**
   - **Host**: `localhost`
   - **Port**: `5432`
   - **Database**: `GifCampDb`
   - **Username**: `chris`
   - **Password**: *(leave this field empty)*
   - **Show all databases**: Uncheck (optional)

5. **SSL Configuration** (if available)
   - Switch to the "SSL" tab
   - Set SSL mode to "Disable" (or "allow" for local development)

6. **Test Connection**
   - Click "Test Connection" button
   - If this is your first time connecting to PostgreSQL, DBeaver may prompt you to download the PostgreSQL JDBC driver
   - Click "Download" if prompted
   - After download, click "Test Connection" again
   - You should see: "Connected" message

7. **Finish**
   - Click "Finish" to save the connection
   - The connection will appear in your database navigator

8. **Explore Your Database**
   - Expand the connection to see:
     - `GifCampDb` database
     - `Schemas` → `public` → `Tables` → `Users` table
   - You can now browse, query, and edit the Users table

## Troubleshooting

### "Connection refused" error
- Make sure PostgreSQL is running: `brew services list`
- If not running, start it: `brew services start postgresql@16`

### "Authentication failed" error
- Verify your username is `chris` (your macOS username)
- Leave the password field empty
- Check PostgreSQL pg_hba.conf if issues persist

### Driver download issues
- Make sure you have internet connection
- You can manually download the PostgreSQL JDBC driver from:
  https://jdbc.postgresql.org/download/

### Port 5432 not working
- Check if PostgreSQL is using a different port
- Run: `lsof -i :5432` to see what's using the port
- Check PostgreSQL logs: `/opt/homebrew/var/log/postgresql@16.log`

## Quick Test Query

Once connected, try this query to verify everything works:

```sql
SELECT * FROM "Users";
```

This will show all users in your database (should be empty initially).


