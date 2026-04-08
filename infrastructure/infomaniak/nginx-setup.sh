#!/bin/bash
# Setup script for nginx configuration on Infomaniak server
# Run this on the server after deployment as root or with sudo

set -e

echo "=== Setting up Nginx configuration ==="

# Check if running as root
if [ "$EUID" -ne 0 ]; then 
    echo "Please run as root or with sudo"
    exit 1
fi

# Copy configuration files
echo "Copying nginx configuration files..."
cp nginx-frontend.conf /etc/nginx/sites-available/recht-und-ordnung-frontend
cp nginx-api.conf /etc/nginx/sites-available/recht-und-ordnung-api

# Create symlinks
echo "Creating symlinks..."
ln -sf /etc/nginx/sites-available/recht-und-ordnung-frontend /etc/nginx/sites-enabled/
ln -sf /etc/nginx/sites-available/recht-und-ordnung-api /etc/nginx/sites-enabled/

# Test nginx configuration
echo "Testing nginx configuration..."
nginx -t

# Reload nginx
echo "Reloading nginx..."
systemctl reload nginx

echo ""
echo "=== Nginx configuration installed ==="
echo ""
echo "Next steps:"
echo "1. Run SSL certificate setup for frontend:"
echo "   certbot --nginx -d recht-und-ordnung.sandro-raess.ch"
echo ""
echo "2. Run SSL certificate setup for API:"
echo "   certbot --nginx -d recht-und-ordnung-api.sandro-raess.ch"
echo ""
echo "3. Verify SSL auto-renewal is working:"
echo "   certbot renew --dry-run"
