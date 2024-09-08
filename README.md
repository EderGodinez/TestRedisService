# TestRedisService
Simulacion de retos de kebernetes para retos de Mega
## Ejecutar contenedor con redis 
``
docker run --name some-redis -d -p 6379:6379 -v /Desktop/data:/data redis redis-server --save 60 1 --loglevel warning
``
