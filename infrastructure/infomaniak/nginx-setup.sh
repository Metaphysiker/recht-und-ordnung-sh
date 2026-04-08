#!/bin/bash
# Setup script for nginx configuration on Infomaniak server
# Run this on the server after deployment

set -e

echo "=== Setting up Nginx configuration ==="

# Copy configuration files
echo "Copying nginx configuration files..."
sudo cp nginx-frontend.conf /etc/nginx/sites-available/recht-und-ordnung-frontend
sudo cp nginx-api.conf /etc/nginx/sites-available/recht-und-ordnung-api

# Create symlinks
echo "Creating symlinks..."
sudo ln -sf /etc/nginx/sites-available/recht-und-ordnung-frontend /etc/nginx/sites-enabled/
sudo ln -sf /etc/nginx/sites-available/recht-und-ordnung-api /etc/nginx/sites-enabled/

# Test nginx configuration
echo "Testing nginx configuration..."
sudo nginx -t

# Reload nginx
echo "Reloading nginx..."
sudo systemctl reload nginx

echo ""
echo "=== Nginx configuration installed ==="
echo ""
echo "Next steps:"
echo "1. Run SSL certificate setup for frontend:"
echo "   sudo certbot --nginx -d recht-und-ordnung.sandro-raess.ch"
echo ""
echo "2. Run SSL certificate setup for API:"
echo "   sudo certbot --nginx -d recht-und-ordnung-api.sandro-raess.ch"
echo ""
echo "3. Verify SSL auto-renewal is working:"
echo "   sudo certbot renew --dry-run"
