#!/bin/sh

# Replace ApiUrl in appsettings.json with environment variable
if [ -n "$API_URL" ]; then
    echo "Setting ApiUrl to: $API_URL"
    sed -i "s|\"ApiUrl\":.*|\"ApiUrl\": \"$API_URL\"|g" /usr/share/nginx/html/appsettings.json
fi

# Start nginx
nginx -g 'daemon off;'
