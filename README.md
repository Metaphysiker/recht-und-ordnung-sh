# Recht und Ordnung SH

A Blazor WebAssembly application with ASP.NET Core backend and PostgreSQL database.

## Getting Started

### Prerequisites

- Docker and Docker Compose
- .NET 9 SDK (for local development)
- Node.js (for local Blazor development)

### Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd recht-und-ordnung-sh
   ```

2. **Configure environment variables**

   Copy the example environment file:
   ```bash
   cp .env.example .env
   ```

   Edit `.env` and update the values as needed:
   - `API_URL`: The URL where the backend API is accessible (used by frontend)
   - `BLAZOR_WASM_PORT`: Port for the Blazor WebAssembly app (default: 5000)
   - `WEBAPI_PORT`: Port for the ASP.NET Core API (default: 5001)
   - `JWT_KEY`: Secret key for JWT token generation (min 32 characters)
   - `ADMIN_PASSWORD`: Password for the default admin user (s.raess@me.com)

3. **Run with Docker Compose**
   ```bash
   cd infrastructure/development
   docker-compose up --build
   ```

4. **Access the application**
   - Blazor WebAssembly: http://localhost:5000
   - WebAPI: http://localhost:5001
   - Default admin credentials:
     - Email: `s.raess@me.com`
     - Password: Value from `ADMIN_PASSWORD` (default: `password`)

## Project Structure

```
├── blazor-wasm/          # Blazor WebAssembly frontend
├── webapi/               # ASP.NET Core Web API backend
├── infrastructure/       # Docker and deployment configs
└── .env                  # Environment variables (not in git)
```

## Development

### Local Development (without Docker)

1. **Start PostgreSQL**
   ```bash
   docker run -d -p 5432:5432 -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=recht_und_ordnung_db postgres:16-alpine
   ```

2. **Run the WebAPI**
   ```bash
   cd webapi
   dotnet run
   ```

3. **Run the Blazor WebAssembly app**
   ```bash
   cd blazor-wasm
   dotnet run
   ```

## Technologies

- **Frontend**: Blazor WebAssembly, MudBlazor
- **Backend**: ASP.NET Core 9, Entity Framework Core
- **Database**: PostgreSQL
- **Authentication**: JWT Bearer tokens
- **Hosting**: Nginx (for Blazor), Kestrel (for API)
