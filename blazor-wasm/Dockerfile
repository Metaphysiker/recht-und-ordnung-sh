# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY blazor-wasm.csproj .
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Runtime stage - serve with nginx
FROM nginx:alpine
WORKDIR /usr/share/nginx/html

# Copy published files
COPY --from=build /app/publish/wwwroot .

# Copy nginx config
COPY nginx.conf /etc/nginx/nginx.conf

EXPOSE 80
