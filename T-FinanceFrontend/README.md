T-Finance Frontend
===================

Overview
--------
This directory contains the frontend application built with Vite + React. The repository is configured for production builds and Docker-based deployment.

Quick commands
--------------
- Install dependencies (Node 25 recommended):

```powershell
npm ci
```

- Dev server:

```powershell
npm run dev
```

- Production build:

```powershell
npm run build:prod
```

- Build and run production image locally:

```powershell
docker build -t t-finance-frontend:prod .
docker run --rm -p 80:80 t-finance-frontend:prod
```

Environment variables
---------------------
- `VITE_API_BASE_URL` — used at build time to bake base API path into code (e.g. `/api` or full URL). Set as Docker build-arg or CI environment variable.
- `VITE_BASE_PATH` — optional base path if app is served from a sub-path (e.g. `/app/`).

Why these changes
------------------
- Multi-stage Dockerfile reduces image size and keeps build-time tools out of the runtime image.
- Pinning Node and using `npm ci` makes builds reproducible.
- `.dockerignore` keeps Docker context small which speeds up builds.
- Vite config uses hashed filenames to enable long-term caching; nginx is configured to serve immutable assets.

Deployment notes
----------------
- Provide SSL certs mounted to `/etc/nginx/ssl` when running with HTTPS.
- Use orchestration (Docker Compose / Kubernetes) to link frontend nginx proxy to backend service (example `proxy_pass http://backend:8080` in `nginx.conf`).
