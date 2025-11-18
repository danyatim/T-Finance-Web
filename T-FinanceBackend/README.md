T-Finance Backend
=================

This folder contains the ASP.NET Core backend for T-Finance. The setup is prepared for production deployment on Ubuntu 24.04 LTS using Docker (recommended) or systemd + reverse proxy (nginx).

Quick build & run (Docker)
--------------------------
Build image locally:

```powershell
cd T-FinanceBackend
docker build -t t-finance-backend:prod .
```

Run container (example):

```powershell
docker run --rm -p 8080:8080 \
  -e JWT_ISSUER=TFinanceBackend \
  -e JWT_AUDIENCE=TFinanceFrontend \
  -e JWT_KEY="your-strong-secret" \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -v /srv/t-finance/data:/app/Data \
  t-finance-backend:prod
```

Notes for Ubuntu 24.04 production
---------------------------------
- Use Docker (or Docker Compose) and run the frontend nginx to proxy to the backend at `http://backend:8080`.
- Obtain TLS certificates for `t-finance-web.ru` with `certbot` and mount them into frontend nginx container at `/etc/nginx/ssl`.
- Do NOT store secrets in `appsettings.json` in the repo. Use environment variables for JWT keys, SMTP, and YooKassa credentials.

Required environment variables (examples):
- `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_KEY` — for JWT signing/validation.
- `DATA_PROTECTION_CERT_PATH` and `DATA_PROTECTION_CERT_PASSWORD` — optional PKCS#12 certificate mounted in container to protect data-protection keys.
- DB connection string: set `ConnectionStrings__Default` environment variable if you want a different DB (e.g., Postgres/SQL Server) or mount `Data/` directory for SQLite.

Systemd example (if not using Docker)
------------------------------------
Create `/etc/systemd/system/t-finance-backend.service`:

```ini
[Unit]
Description=T-Finance Backend
After=network.target

[Service]
WorkingDirectory=/var/www/t-finance-backend
ExecStart=/usr/bin/dotnet /var/www/t-finance-backend/TFinanceBackend.dll
Restart=always
RestartSec=10
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:8080

[Install]
WantedBy=multi-user.target
```

Then reload systemd and start:

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now t-finance-backend
```

Domain and nginx
----------------
- The frontend nginx is configured to terminate TLS for `t-finance-web.ru` and proxy `/api` requests to `http://backend:8080` (Docker Compose service name `backend`). Adjust `proxy_pass` if you run backend elsewhere.

Security
--------
- Keep the DataProtection keys on a persistent volume and, if possible, encrypt them with a certificate (mount PKCS#12 into container).
- Rotate JWT keys periodically and avoid embedding them in images.
