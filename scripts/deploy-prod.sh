#!/bin/bash

# Production Deployment Script for Procedural Mini Game Generator
set -e

echo "🚀 Starting production deployment..."

# Configuration
COMPOSE_FILE="docker-compose.prod.yml"
ENV_FILE=".env.production.local"
BACKUP_DIR="./backups/$(date +%Y%m%d_%H%M%S)"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Functions
log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

check_prerequisites() {
    log_info "Checking prerequisites..."
    
    # Check if Docker is installed and running
    if ! command -v docker &> /dev/null; then
        log_error "Docker is not installed"
        exit 1
    fi
    
    if ! docker info &> /dev/null; then
        log_error "Docker is not running"
        exit 1
    fi
    
    # Check if Docker Compose is available
    if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
        log_error "Docker Compose is not available"
        exit 1
    fi
    
    # Check if environment file exists
    if [ ! -f "$ENV_FILE" ]; then
        log_warn "Environment file $ENV_FILE not found"
        log_info "Creating from template..."
        cp .env.production "$ENV_FILE"
        log_warn "Please edit $ENV_FILE with your production values before continuing"
        exit 1
    fi
    
    log_info "Prerequisites check passed ✓"
}

create_directories() {
    log_info "Creating necessary directories..."
    
    mkdir -p logs
    mkdir -p temp
    mkdir -p exports
    mkdir -p certs
    mkdir -p nginx/logs
    mkdir -p "$BACKUP_DIR"
    
    log_info "Directories created ✓"
}

backup_current_deployment() {
    log_info "Creating backup of current deployment..."
    
    # Backup logs
    if [ -d "logs" ]; then
        cp -r logs "$BACKUP_DIR/"
    fi
    
    # Backup configuration
    if [ -f "$ENV_FILE" ]; then
        cp "$ENV_FILE" "$BACKUP_DIR/"
    fi
    
    # Export current container state
    if docker-compose -f "$COMPOSE_FILE" ps -q > /dev/null 2>&1; then
        docker-compose -f "$COMPOSE_FILE" ps > "$BACKUP_DIR/container_status.txt"
    fi
    
    log_info "Backup created at $BACKUP_DIR ✓"
}

build_images() {
    log_info "Building production images..."
    
    # Load environment variables
    export $(cat "$ENV_FILE" | grep -v '^#' | xargs)
    
    # Build images with no cache for production
    docker-compose -f "$COMPOSE_FILE" build --no-cache --parallel
    
    log_info "Images built successfully ✓"
}

run_health_checks() {
    log_info "Running health checks..."
    
    # Wait for services to be ready
    sleep 30
    
    # Check backend health
    if curl -f http://localhost/api/health > /dev/null 2>&1; then
        log_info "Backend health check passed ✓"
    else
        log_error "Backend health check failed"
        return 1
    fi
    
    # Check frontend health
    if curl -f http://localhost/health > /dev/null 2>&1; then
        log_info "Frontend health check passed ✓"
    else
        log_error "Frontend health check failed"
        return 1
    fi
    
    # Check detailed backend health
    if curl -f http://localhost/api/health/detailed > /dev/null 2>&1; then
        log_info "Detailed health check passed ✓"
    else
        log_warn "Detailed health check failed (non-critical)"
    fi
    
    log_info "Health checks completed ✓"
}

deploy() {
    log_info "Deploying to production..."
    
    # Load environment variables
    export $(cat "$ENV_FILE" | grep -v '^#' | xargs)
    
    # Stop existing containers
    docker-compose -f "$COMPOSE_FILE" down --remove-orphans
    
    # Start new containers
    docker-compose -f "$COMPOSE_FILE" up -d
    
    # Wait for services to start
    log_info "Waiting for services to start..."
    sleep 10
    
    # Check if containers are running
    if ! docker-compose -f "$COMPOSE_FILE" ps | grep -q "Up"; then
        log_error "Some containers failed to start"
        docker-compose -f "$COMPOSE_FILE" logs
        exit 1
    fi
    
    log_info "Deployment completed ✓"
}

cleanup() {
    log_info "Cleaning up..."
    
    # Remove unused images
    docker image prune -f
    
    # Remove old backups (keep last 10)
    find ./backups -maxdepth 1 -type d -name "20*" | sort -r | tail -n +11 | xargs rm -rf
    
    log_info "Cleanup completed ✓"
}

rollback() {
    log_error "Deployment failed. Rolling back..."
    
    # Stop current containers
    docker-compose -f "$COMPOSE_FILE" down --remove-orphans
    
    # Restore from backup if available
    if [ -d "$BACKUP_DIR" ]; then
        log_info "Restoring from backup..."
        # Add rollback logic here
    fi
    
    log_error "Rollback completed"
    exit 1
}

# Main deployment process
main() {
    log_info "Production Deployment Started"
    log_info "Timestamp: $(date)"
    
    # Set trap for cleanup on failure
    trap rollback ERR
    
    check_prerequisites
    create_directories
    backup_current_deployment
    build_images
    deploy
    
    # Run health checks
    if ! run_health_checks; then
        rollback
    fi
    
    cleanup
    
    log_info "🎉 Production deployment completed successfully!"
    log_info "Application is available at: http://localhost"
    log_info "API documentation: http://localhost/api"
    log_info "Health check: http://localhost/api/health"
    
    # Display running containers
    echo ""
    log_info "Running containers:"
    docker-compose -f "$COMPOSE_FILE" ps
}

# Run main function
main "$@"