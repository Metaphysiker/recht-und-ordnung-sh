# Deployment to Infomaniak Server

This directory contains the deployment configuration for the Infomaniak server at 84.234.19.192.

## Files

- **docker-compose.build.yml** - Builds production Docker images locally
- **docker-compose.remote.yml** - Runs containers on the remote server
- **.env.example** - Template for environment variables (copy to `.env`)
- **appsettings.Production.json** - Production API configuration template
- **deploy-to-infomaniak-server.sh** - Automated deployment script

## Prerequisites

1. SSH access to deploy@84.234.19.192
2. Docker installed locally
3. `pv` command installed (for progress display): `sudo apt install pv`

## Setup

### First Time Setup

1. Copy the environment template:
   ```bash
   cd infrastructure/infomaniak
   cp .env.example .env
   ```

2. Edit `.env` with your production values:
   - Database credentials
   - JWT secret key (generate with: `openssl rand -base64 32`)
   - Admin password
   - SMTP configuration
   - Your domain URLs

3. Create the deployment directory on the server:
   ```bash
   ssh deploy@84.234.19.192 "mkdir -p /home/deploy/recht-und-ordnung-sh"
   ```

4. Ensure PostgreSQL data directory permissions:
   ```bash
   ssh deploy@84.234.19.192 "sudo mkdir -p /home/deploy/recht-und-ordnung-sh/postgres-data && sudo chown -R 999:999 /home/deploy/recht-und-ordnung-sh/postgres-data"
   ```

## Deployment

Run the deployment script:

```bash
cd infrastructure/infomaniak
./deploy-to-infomaniak-server.sh
```

The script will:
1. Check for uncommitted changes
2. Create deployment tracking (branch + tag)
3. Build Docker images locally
4. Transfer images to the server
5. Transfer configuration files (including nginx configs)
6. Restart containers on the server
7. Clean up old Docker resources

### First-Time Nginx Setup

After the first deployment, set up nginx and SSL certificates on the server.

**As root user:**

```bash
ssh root@84.234.19.192
cd /home/deploy/recht-und-ordnung-sh
./nginx-setup.sh
```

This will:
1. Install nginx configuration files
2. Enable the sites
3. Test and reload nginx

Then set up SSL certificates with Certbot:

```bash
# Frontend SSL
certbot --nginx -d recht-und-ordnung.sandro-raess.ch

# API SSL
certbot --nginx -d recht-und-ordnung-api.sandro-raess.ch

# Test auto-renewal
certbot renew --dry-run
```

## Manual Operations

### Check current deployment

```bash
ssh deploy@84.234.19.192 cat /home/deploy/recht-und-ordnung-sh/deployment.txt
```

### View logs

```bash
ssh deploy@84.234.19.192 "cd /home/deploy/recht-und-ordnung-sh && docker compose --file docker-compose.remote.yml logs -f"
```

### Restart services

```bash
ssh deploy@84.234.19.192 "cd /home/deploy/recht-und-ordnung-sh && docker compose --file docker-compose.remote.yml restart"
```

### Stop services

```bash
ssh deploy@84.234.19.192 "cd /home/deploy/recht-und-ordnung-sh && docker compose --file docker-compose.remote.yml down"
```

### Database migrations

Run migrations on the server:

```bash
ssh deploy@84.234.19.192 "cd /home/deploy/recht-und-ordnung-sh && docker compose --file docker-compose.remote.yml exec webapi dotnet ef database update"
```

## Deployment History

View deployment history:

```bash
# List deployment branches
git branch --list 'deployment/*'

# List deployment tags
git tag --list 'deploy-*'

# Show deployment details
git show deploy-<timestamp>
```

## Port Configuration

Default ports (configurable in `.env`):
- Blazor WASM: 8070 (internal - nginx forwards to this)
- WebAPI: 8071 (internal - nginx forwards to this)
- PostgreSQL: Internal only (not exposed)

**Firewall configuration:**
- External: Only ports 80 (HTTP) and 443 (HTTPS) need to be open
- Internal: Ports 8070 and 8071 should only be accessible from localhost
- Nginx acts as reverse proxy handling SSL termination

**Nginx domains:**
- Frontend: recht-und-ordnung.sandro-raess.ch → http://localhost:8070
- API: recht-und-ordnung-api.sandro-raess.ch → http://localhost:8071

## Troubleshooting

### Container won't start

Check logs:
```bash
ssh deploy@84.234.19.192 "docker logs recht-und-ordnung-sh-webapi"
ssh deploy@84.234.19.192 "docker logs recht-und-ordnung-sh-blazor"
```

### Database connection issues

Verify PostgreSQL is running:
```bash
ssh deploy@84.234.19.192 "docker ps | grep postgres"
```

Check connection string in webapi container:
```bash
ssh deploy@84.234.19.192 "docker exec recht-und-ordnung-sh-webapi env | grep ConnectionStrings"
```

### Image transfer is slow

The script uses `bzip2` compression and `pv` for progress. For faster transfers over good connections, you can modify the script to use `gzip` instead:
```bash
docker save image-name | gzip | pv | ssh deploy@84.234.19.192 docker load
```
