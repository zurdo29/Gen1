@echo off
echo Starting development environment...

echo Building and starting containers...
docker-compose -f docker-compose.dev.yml up --build

pause