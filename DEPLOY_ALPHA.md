# Pepino — publicar la alpha para probar con amigos

La alpha tiene dos procesos:

- frontend React + Three.js: `http://localhost:5174`
- backend ASP.NET + SignalR: `http://localhost:5264`

SignalR necesita que el navegador del amigo pueda alcanzar el backend. Publicar solamente el frontend no alcanza.

## Opción recomendada: Render

Render permite crear un Static Site para el frontend y un Web Service para ASP.NET. Sus Web Services soportan WebSockets, que es lo que usa SignalR. El repo incluye `render.yaml`: se puede crear todo desde **New > Blueprint**. Documentación: [primer deploy en Render](https://render.com/docs/your-first-deploy) y [WebSockets en Render](https://render.com/docs/websocket).

### Backend

Crear un **Web Service** conectado al repositorio:

- Root Directory: `Back/GameServer/GameServer`
- Runtime: `Docker`
- Dockerfile: `Back/GameServer/GameServer/Dockerfile`

El servicio debe escuchar en `0.0.0.0` y en `$PORT`; Render necesita detectar ese puerto. Guardar la URL pública, por ejemplo `https://pepino-api.onrender.com`.

Variable `PEPINO_FRONTEND_ORIGINS`: ingresar la URL pública del frontend, por ejemplo `https://pepino-web.onrender.com`.

Antes de publicar, reemplazar el CORS abierto de desarrollo por el dominio exacto del frontend. Para esta alpha se puede configurar `PEPINO_FRONTEND_ORIGIN` y usarlo en `Program.cs`.

### Frontend

Crear un **Static Site**:

- Root Directory: `Front/game-client`
- Build Command: `npm ci && npm run build`
- Publish Directory: `dist`
- Environment Variable: `VITE_GAME_HUB_URL=https://pepino-api.onrender.com/gamehub`

La configuración de Vite ya lee `VITE_GAME_HUB_URL`. En producción se debe usar HTTPS; el cliente SignalR elegirá WebSockets cuando estén disponibles y hará fallback a transporte compatible.

La URL que se comparte será la del Static Site, por ejemplo `https://pepino-web.onrender.com`.

### Prueba de publicación

1. Abrir la URL pública desde una ventana incógnito.
2. Abrir la misma URL desde el teléfono o la PC del amigo.
3. Usar el mismo código de sala.
4. Comprobar lobby, mínimo de dos jugadores, reparto, turno del 3♦, jugada, pase, pepineado, comodín y reconexión.
5. Mirar los logs del Web Service si el frontend carga pero aparece “No se pudo conectar”.

Render puede dormir servicios gratuitos cuando no hay tráfico; el primer acceso puede tardar. Las salas y partidas siguen siendo temporales porque el backend actual guarda todo en memoria.

## Opción rápida desde la PC: Cloudflare Tunnel

Cloudflare Tunnel puede publicar un servicio local sin abrir puertos del router. La documentación oficial muestra el flujo `cloudflared tunnel --url http://localhost:8080`: [Set up Cloudflare Tunnel](https://developers.cloudflare.com/tunnel/setup/).

Para una prueba descartable se necesitan dos URLs públicas:

```powershell
# Terminal 1 — backend
cd C:\MisRepos\PepinoWeb\Back\GameServer\GameServer
dotnet run --urls http://localhost:5264

# Terminal 2 — frontend
cd C:\MisRepos\PepinoWeb\Front\game-client
 $env:VITE_GAME_HUB_URL="https://URL_PUBLICA_DEL_BACKEND/gamehub"
node node_modules/vite/bin/vite.js --host 0.0.0.0

# Terminal 3 — backend público
cloudflared tunnel --url http://localhost:5264

# Terminal 4 — frontend público
cloudflared tunnel --url http://localhost:5174
```

Reemplazar `URL_PUBLICA_DEL_BACKEND` por la dirección HTTPS entregada por el túnel del backend y reiniciar Vite después de definir la variable. Compartir la dirección HTTPS del túnel del frontend.

Estas URLs temporales cambian y el frontend debe seguir corriendo en tu PC. Para una dirección permanente se configura un túnel nombrado y un dominio propio, con dos rutas: una para el frontend y otra para `/gamehub`.

## Lo que falta antes de una publicación pública seria

- CORS restringido al dominio del frontend.
- HTTPS y URL de backend por variables de entorno.
- Persistencia de salas/cuentas si se necesitan después de reiniciar.
- Autenticación, rate limiting y logs sin datos privados.
- Control de errores y límites de tamaño de sala.
- No subir `node_modules`, `dist`, `bin`, `obj`, capturas de prueba ni `FPHands.unitypackage` al repositorio.

## Estado actual

El frontend compila con TypeScript y Vite, el backend compila con .NET y las pruebas de reglas pasan. La publicación todavía no se hizo porque requiere elegir una cuenta/proveedor y, para Render, conectar el repositorio. Para compartir la alpha hoy, usar el túnel; para una URL más estable, crear los dos servicios en Render.
