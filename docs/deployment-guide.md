# Web Level Editor - Deployment and Configuration Guide

## Table of Contents
1. [System Requirements](#system-requirements)
2. [Development Environment Setup](#development-environment-setup)
3. [Production Deployment](#production-deployment)
4. [Configuration Management](#configuration-management)
5. [Security Configuration](#security-configuration)
6. [Performance Optimization](#performance-optimization)
7. [Monitoring and Logging](#monitoring-and-logging)
8. [Backup and Recovery](#backup-and-recovery)
9. [Troubleshooting Deployment Issues](#troubleshooting-deployment-issues)

## System Requirements

### Minimum Requirements

**Server Hardware:**
- CPU: 2 cores, 2.4 GHz
- RAM: 4 GB
- Storage: 20 GB SSD
- Network: 100 Mbps

**Software Requirements:**
- .NET 8.0 Runtime
- Node.js 18+ (for frontend build)
- Docker 20.10+ (recommended)
- Reverse proxy (Nginx/Apache)

### Recommended Requirements

**Server Hardware:**
- CPU: 4+ cores, 3.0+ GHz
- RAM: 8+ GB
- Storage: 50+ GB SSD
- Network: 1 Gbps

**Software Stack:**
- .NET 8.0 SDK
- Node.js 20+
- Docker Compose
- Load balancer
- CDN integration

### Supported Platforms

**Operating Systems:**
- Linux (Ubuntu 20.04+, CentOS 8+, RHEL 8+)
- Windows Server 2019+
- macOS 12+ (development only)

**Cloud Platforms:**
- Azure App Service
- AWS Elastic Beanstalk
- Google Cloud Run
- DigitalOcean App Platform
- Heroku

## Development Environment Setup

### Local Development with Docker

**1. Clone the Repository**
```bash
git clone https://github.com/your-org/web-level-editor.git
cd web-level-editor
```

**2. Environment Configuration**
```bash
# Copy environment templates
cp .env.example .env.development
cp frontend/.env.development.example frontend/.env.development

# Edit configuration files
nano .env.development
nano frontend/.env.development
```

**3. Docker Compose Setup**
```yaml
# docker-compose.dev.yml
version: '3.8'
services:
  backend:
    build:
      context: ./backend
      dockerfile: Dockerfile.dev
    ports:
      - "5000:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:80
      - ConnectionStrings__DefaultConnection=${DB_CONNECTION_STRING}
    volumes:
      - ./backend:/app
      - /app/bin
      - /app/obj
    depends_on:
      - redis

  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile.dev
    ports:
      - "3000:3000"
    environment:
      - REACT_APP_API_URL=http://localhost:5000
      - REACT_APP_ENVIRONMENT=development
    volumes:
      - ./frontend:/app
      - /app/node_modules

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data

volumes:
  redis_data:
```

**4. Start Development Environment**
```bash
docker-compose -f docker-compose.dev.yml up -d
```

### Manual Development Setup

**Backend Setup:**
```bash
cd backend/ProceduralMiniGameGenerator.WebAPI
dotnet restore
dotnet build
dotnet run --environment Development
```

**Frontend Setup:**
```bash
cd frontend
npm install
npm run dev
```

### Development Configuration

**Backend Configuration (appsettings.Development.json):**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=leveleditor.db",
    "Redis": "localhost:6379"
  },
  "GenerationSettings": {
    "MaxLevelSize": 200,
    "DefaultTimeout": 30,
    "EnableCaching": true,
    "CacheExpirationMinutes": 60
  },
  "ApiSettings": {
    "RateLimit": {
      "RequestsPerMinute": 100,
      "RequestsPerHour": 1000
    },
    "Cors": {
      "AllowedOrigins": ["http://localhost:3000"],
      "AllowCredentials": true
    }
  }
}
```

**Frontend Configuration (.env.development):**
```env
REACT_APP_API_URL=http://localhost:5000
REACT_APP_ENVIRONMENT=development
REACT_APP_ENABLE_DEBUG=true
REACT_APP_LOG_LEVEL=debug
REACT_APP_CACHE_ENABLED=true
```

## Production Deployment

### Docker Production Deployment

**1. Production Docker Compose**
```yaml
# docker-compose.prod.yml
version: '3.8'
services:
  backend:
    build:
      context: ./backend
      dockerfile: Dockerfile.prod
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:80
    volumes:
      - ./logs:/app/logs
      - ./data:/app/data
    restart: unless-stopped
    depends_on:
      - redis

  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile.prod
    restart: unless-stopped

  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx/nginx.prod.conf:/etc/nginx/nginx.conf
      - ./ssl:/etc/nginx/ssl
      - ./logs/nginx:/var/log/nginx
    depends_on:
      - backend
      - frontend
    restart: unless-stopped

  redis:
    image: redis:7-alpine
    volumes:
      - redis_data:/data
    restart: unless-stopped
    command: redis-server --appendonly yes

volumes:
  redis_data:
```

**2. Production Dockerfiles**

**Backend Dockerfile.prod:**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ProceduralMiniGameGenerator.WebAPI.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProceduralMiniGameGenerator.WebAPI.dll"]
```

**Frontend Dockerfile.prod:**
```dockerfile
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci --only=production
COPY . .
RUN npm run build

FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.prod.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

**3. Nginx Configuration**
```nginx
# nginx/nginx.prod.conf
upstream backend {
    server backend:80;
}

upstream frontend {
    server frontend:80;
}

server {
    listen 80;
    server_name your-domain.com;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name your-domain.com;

    ssl_certificate /etc/nginx/ssl/cert.pem;
    ssl_certificate_key /etc/nginx/ssl/key.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-RSA-AES256-GCM-SHA512:DHE-RSA-AES256-GCM-SHA512;

    # Frontend
    location / {
        proxy_pass http://frontend;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # API
    location /api/ {
        proxy_pass http://backend;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        
        # WebSocket support
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        
        # Timeouts
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }

    # Static assets caching
    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg)$ {
        proxy_pass http://frontend;
        expires 1y;
        add_header Cache-Control "public, immutable";
    }
}
```

### Cloud Platform Deployment

#### Azure App Service

**1. Azure CLI Deployment**
```bash
# Login to Azure
az login

# Create resource group
az group create --name leveleditor-rg --location eastus

# Create App Service plan
az appservice plan create \
  --name leveleditor-plan \
  --resource-group leveleditor-rg \
  --sku B1 \
  --is-linux

# Create web app
az webapp create \
  --resource-group leveleditor-rg \
  --plan leveleditor-plan \
  --name leveleditor-app \
  --deployment-container-image-name your-registry/leveleditor:latest
```

**2. Azure Configuration**
```bash
# Set environment variables
az webapp config appsettings set \
  --resource-group leveleditor-rg \
  --name leveleditor-app \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__Redis="your-redis-connection-string"
```

#### AWS Elastic Beanstalk

**1. EB CLI Deployment**
```bash
# Initialize EB application
eb init leveleditor --platform docker --region us-east-1

# Create environment
eb create production --instance-type t3.medium

# Deploy application
eb deploy
```

**2. Dockerrun.aws.json**
```json
{
  "AWSEBDockerrunVersion": 2,
  "containerDefinitions": [
    {
      "name": "backend",
      "image": "your-registry/leveleditor-backend:latest",
      "memory": 512,
      "portMappings": [
        {
          "hostPort": 5000,
          "containerPort": 80
        }
      ]
    },
    {
      "name": "frontend",
      "image": "your-registry/leveleditor-frontend:latest",
      "memory": 256,
      "portMappings": [
        {
          "hostPort": 3000,
          "containerPort": 80
        }
      ]
    }
  ]
}
```

## Configuration Management

### Environment Variables

**Backend Environment Variables:**
```env
# Core Settings
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:80

# Database
ConnectionStrings__DefaultConnection=Data Source=leveleditor.db
ConnectionStrings__Redis=localhost:6379

# Generation Settings
GenerationSettings__MaxLevelSize=200
GenerationSettings__DefaultTimeout=30
GenerationSettings__EnableCaching=true

# API Settings
ApiSettings__RateLimit__RequestsPerMinute=60
ApiSettings__RateLimit__RequestsPerHour=1000

# Security
JwtSettings__SecretKey=your-secret-key
JwtSettings__ExpirationHours=24

# Logging
Logging__LogLevel__Default=Information
Logging__LogLevel__Microsoft=Warning
```

**Frontend Environment Variables:**
```env
# API Configuration
REACT_APP_API_URL=https://api.your-domain.com
REACT_APP_ENVIRONMENT=production

# Feature Flags
REACT_APP_ENABLE_DEBUG=false
REACT_APP_ENABLE_ANALYTICS=true
REACT_APP_ENABLE_OFFLINE=true

# Performance
REACT_APP_CACHE_ENABLED=true
REACT_APP_MAX_CACHE_SIZE=100MB
```

### Configuration Files

**Production appsettings.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Error",
      "ProceduralMiniGameGenerator": "Information"
    },
    "Console": {
      "IncludeScopes": false
    },
    "File": {
      "Path": "/app/logs/app-.log",
      "RollingInterval": "Day",
      "RetainedFileCountLimit": 30
    }
  },
  "AllowedHosts": "*",
  "GenerationSettings": {
    "MaxLevelSize": 500,
    "DefaultTimeout": 60,
    "EnableCaching": true,
    "CacheExpirationMinutes": 120,
    "MaxConcurrentGenerations": 10
  },
  "ApiSettings": {
    "RateLimit": {
      "RequestsPerMinute": 60,
      "RequestsPerHour": 1000,
      "EnableRateLimiting": true
    },
    "Cors": {
      "AllowedOrigins": ["https://your-domain.com"],
      "AllowCredentials": false
    },
    "Swagger": {
      "EnableInProduction": false
    }
  },
  "SecuritySettings": {
    "RequireHttps": true,
    "EnableHsts": true,
    "HstsMaxAge": 31536000
  }
}
```

## Security Configuration

### HTTPS and SSL

**1. SSL Certificate Setup**
```bash
# Using Let's Encrypt with Certbot
sudo apt install certbot python3-certbot-nginx
sudo certbot --nginx -d your-domain.com
```

**2. Security Headers**
```nginx
# Add to nginx configuration
add_header X-Frame-Options "SAMEORIGIN" always;
add_header X-Content-Type-Options "nosniff" always;
add_header X-XSS-Protection "1; mode=block" always;
add_header Referrer-Policy "strict-origin-when-cross-origin" always;
add_header Content-Security-Policy "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';" always;
```

### API Security

**1. Rate Limiting Configuration**
```csharp
// Startup.cs
services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("ApiPolicy", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 10;
    });
});
```

**2. CORS Configuration**
```csharp
services.AddCors(options =>
{
    options.AddPolicy("ProductionPolicy", builder =>
    {
        builder
            .WithOrigins("https://your-domain.com")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .SetIsOriginAllowedToAllowWildcardSubdomains();
    });
});
```

### Input Validation

**1. Model Validation**
```csharp
public class GenerationConfigValidator : AbstractValidator<GenerationConfig>
{
    public GenerationConfigValidator()
    {
        RuleFor(x => x.Terrain.Width)
            .InclusiveBetween(10, 500)
            .WithMessage("Width must be between 10 and 500");
            
        RuleFor(x => x.Terrain.Height)
            .InclusiveBetween(10, 500)
            .WithMessage("Height must be between 10 and 500");
            
        RuleFor(x => x.Entities.Density)
            .InclusiveBetween(0.0f, 1.0f)
            .WithMessage("Entity density must be between 0.0 and 1.0");
    }
}
```

## Performance Optimization

### Caching Strategy

**1. Redis Configuration**
```csharp
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = connectionString;
    options.InstanceName = "LevelEditor";
});

services.AddMemoryCache(options =>
{
    options.SizeLimit = 100; // MB
    options.CompactionPercentage = 0.25;
});
```

**2. Response Caching**
```csharp
[ResponseCache(Duration = 3600, VaryByQueryKeys = new[] { "config" })]
public async Task<IActionResult> GenerateLevel([FromBody] GenerationConfig config)
{
    // Implementation
}
```

### Database Optimization

**1. Connection Pooling**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=leveleditor.db;Pooling=true;Max Pool Size=100;Min Pool Size=5;"
  }
}
```

**2. Query Optimization**
```csharp
// Use async methods
var levels = await context.Levels
    .Where(l => l.CreatedAt > DateTime.UtcNow.AddDays(-30))
    .OrderByDescending(l => l.CreatedAt)
    .Take(50)
    .ToListAsync();
```

### Frontend Optimization

**1. Build Optimization**
```javascript
// vite.config.ts
export default defineConfig({
  build: {
    rollupOptions: {
      output: {
        manualChunks: {
          vendor: ['react', 'react-dom'],
          ui: ['@mui/material', '@mui/icons-material']
        }
      }
    },
    chunkSizeWarningLimit: 1000
  }
});
```

**2. Service Worker Configuration**
```javascript
// sw.js
const CACHE_NAME = 'leveleditor-v1';
const urlsToCache = [
  '/',
  '/static/js/bundle.js',
  '/static/css/main.css'
];

self.addEventListener('install', event => {
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then(cache => cache.addAll(urlsToCache))
  );
});
```

## Monitoring and Logging

### Application Monitoring

**1. Health Checks**
```csharp
services.AddHealthChecks()
    .AddCheck("database", () => HealthCheckResult.Healthy())
    .AddCheck("redis", () => HealthCheckResult.Healthy())
    .AddCheck("generation-service", () => HealthCheckResult.Healthy());

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

**2. Metrics Collection**
```csharp
services.AddApplicationInsightsTelemetry();

// Custom metrics
public class GenerationMetrics
{
    private readonly IMetrics _metrics;
    
    public void RecordGenerationTime(TimeSpan duration)
    {
        _metrics.CreateHistogram<double>("generation_duration_seconds")
            .Record(duration.TotalSeconds);
    }
}
```

### Logging Configuration

**1. Structured Logging**
```csharp
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.AddFile("/app/logs/app-.log", options =>
    {
        options.RollingInterval = RollingInterval.Day;
        options.RetainedFileCountLimit = 30;
    });
});
```

**2. Log Aggregation**
```yaml
# docker-compose.monitoring.yml
version: '3.8'
services:
  elasticsearch:
    image: docker.elastic.co/elasticsearch/elasticsearch:8.5.0
    environment:
      - discovery.type=single-node
      - xpack.security.enabled=false
    ports:
      - "9200:9200"

  kibana:
    image: docker.elastic.co/kibana/kibana:8.5.0
    ports:
      - "5601:5601"
    depends_on:
      - elasticsearch

  filebeat:
    image: docker.elastic.co/beats/filebeat:8.5.0
    volumes:
      - ./logs:/var/log/app
      - ./filebeat.yml:/usr/share/filebeat/filebeat.yml
    depends_on:
      - elasticsearch
```

## Backup and Recovery

### Database Backup

**1. Automated Backup Script**
```bash
#!/bin/bash
# backup.sh

BACKUP_DIR="/backups"
DATE=$(date +%Y%m%d_%H%M%S)
DB_FILE="/app/data/leveleditor.db"

# Create backup
cp "$DB_FILE" "$BACKUP_DIR/leveleditor_$DATE.db"

# Compress backup
gzip "$BACKUP_DIR/leveleditor_$DATE.db"

# Clean old backups (keep 30 days)
find "$BACKUP_DIR" -name "leveleditor_*.db.gz" -mtime +30 -delete

echo "Backup completed: leveleditor_$DATE.db.gz"
```

**2. Backup Cron Job**
```bash
# Add to crontab
0 2 * * * /path/to/backup.sh >> /var/log/backup.log 2>&1
```

### Configuration Backup

**1. Configuration Versioning**
```bash
#!/bin/bash
# config-backup.sh

CONFIG_DIR="/app/config"
BACKUP_DIR="/backups/config"
DATE=$(date +%Y%m%d_%H%M%S)

# Create timestamped backup
tar -czf "$BACKUP_DIR/config_$DATE.tar.gz" -C "$CONFIG_DIR" .

# Keep last 10 backups
ls -t "$BACKUP_DIR"/config_*.tar.gz | tail -n +11 | xargs rm -f
```

### Disaster Recovery

**1. Recovery Procedures**
```bash
#!/bin/bash
# restore.sh

BACKUP_FILE=$1
RESTORE_DIR="/app/data"

if [ -z "$BACKUP_FILE" ]; then
    echo "Usage: $0 <backup_file>"
    exit 1
fi

# Stop services
docker-compose down

# Restore database
gunzip -c "$BACKUP_FILE" > "$RESTORE_DIR/leveleditor.db"

# Start services
docker-compose up -d

echo "Restore completed from $BACKUP_FILE"
```

## Troubleshooting Deployment Issues

### Common Deployment Problems

**1. Container Startup Issues**
```bash
# Check container logs
docker logs leveleditor-backend
docker logs leveleditor-frontend

# Check container status
docker ps -a

# Inspect container configuration
docker inspect leveleditor-backend
```

**2. Network Connectivity Issues**
```bash
# Test internal network connectivity
docker exec leveleditor-frontend ping backend
docker exec leveleditor-backend ping redis

# Check port bindings
netstat -tlnp | grep :80
netstat -tlnp | grep :443
```

**3. Database Connection Issues**
```bash
# Test database connectivity
docker exec leveleditor-backend dotnet ef database update

# Check database file permissions
ls -la /app/data/leveleditor.db

# Test Redis connectivity
docker exec leveleditor-backend redis-cli ping
```

### Performance Troubleshooting

**1. Memory Issues**
```bash
# Monitor memory usage
docker stats

# Check system memory
free -h
cat /proc/meminfo

# Analyze memory leaks
dotnet-dump collect -p $(pgrep dotnet)
```

**2. CPU Performance**
```bash
# Monitor CPU usage
top -p $(pgrep dotnet)
htop

# Profile application
dotnet-trace collect -p $(pgrep dotnet)
```

### Log Analysis

**1. Error Pattern Detection**
```bash
# Search for errors in logs
grep -i error /app/logs/*.log
grep -i exception /app/logs/*.log

# Analyze request patterns
awk '{print $1}' /var/log/nginx/access.log | sort | uniq -c | sort -nr

# Monitor response times
awk '{print $NF}' /var/log/nginx/access.log | sort -n | tail -10
```

**2. Performance Metrics**
```bash
# Database query analysis
sqlite3 /app/data/leveleditor.db ".timer on" ".explain query plan SELECT * FROM Levels;"

# Redis performance
redis-cli --latency-history -i 1

# Network latency
ping -c 10 your-domain.com
```

### Rollback Procedures

**1. Application Rollback**
```bash
#!/bin/bash
# rollback.sh

PREVIOUS_VERSION=$1

if [ -z "$PREVIOUS_VERSION" ]; then
    echo "Usage: $0 <previous_version>"
    exit 1
fi

# Pull previous version
docker pull your-registry/leveleditor:$PREVIOUS_VERSION

# Update docker-compose
sed -i "s/:latest/:$PREVIOUS_VERSION/g" docker-compose.prod.yml

# Restart services
docker-compose -f docker-compose.prod.yml up -d

echo "Rollback to version $PREVIOUS_VERSION completed"
```

**2. Database Rollback**
```bash
#!/bin/bash
# db-rollback.sh

BACKUP_FILE=$1

# Stop application
docker-compose down

# Restore database
gunzip -c "$BACKUP_FILE" > /app/data/leveleditor.db

# Start application
docker-compose up -d

echo "Database rollback completed"
```

---

## Deployment Checklist

### Pre-Deployment
- [ ] Code review completed
- [ ] Unit tests passing
- [ ] Integration tests passing
- [ ] Security scan completed
- [ ] Performance testing completed
- [ ] Documentation updated
- [ ] Backup procedures tested

### Deployment
- [ ] Environment variables configured
- [ ] SSL certificates installed
- [ ] Database migrations applied
- [ ] Cache warmed up
- [ ] Health checks passing
- [ ] Monitoring configured
- [ ] Logging configured

### Post-Deployment
- [ ] Application functionality verified
- [ ] Performance metrics normal
- [ ] Error rates acceptable
- [ ] User acceptance testing
- [ ] Rollback procedures tested
- [ ] Team notified of deployment
- [ ] Documentation updated

This comprehensive deployment guide provides everything needed to successfully deploy and maintain the Web Level Editor in production environments.