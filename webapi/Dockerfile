# =========================
# Build stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Copy project file
COPY *.csproj ./

# Restore dependencies
RUN dotnet restore

# Copy source
COPY . ./

# Publish
RUN dotnet publish \
    -c Release \
    -r linux-x64 \
    --no-self-contained \
    -o /app/publish

# =========================
# Runtime stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:9.0

WORKDIR /app

# Install native dependencies required by SkiaSharp / QuestPDF
RUN apt-get update && apt-get install -y \
    libfontconfig1 \
    libfreetype6 \
    libx11-6 \
    libxext6 \
    libxrender1 \
    libjpeg62-turbo \
    libpng16-16 \
    && rm -rf /var/lib/apt/lists/*

# Copy published output
COPY --from=build /app/publish .

# ASP.NET Core port
EXPOSE 8080

ENTRYPOINT ["dotnet", "webapi.dll"]
