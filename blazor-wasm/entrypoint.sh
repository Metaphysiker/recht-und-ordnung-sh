#!/bin/sh
set -e

echo "Starting Blazor WebAssembly with API URL: $API_URL"

for file in /usr/share/nginx/html/appsettings*.json; do
  if [ -f "$file" ]; then
    echo "Updating $file"
    sed -i "s|\"ApiUrl\"[[:space:]]*:[[:space:]]*\"[^\"]*\"|\"ApiUrl\": \"$API_URL\"|g" "$file"
  fi
done

exec nginx -g 'daemon off;'
