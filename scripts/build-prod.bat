@echo off
echo Building production containers...

docker-compose -f docker-compose.yml build

echo Production build complete!
pause