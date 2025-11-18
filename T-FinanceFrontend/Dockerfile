## Production multi-stage Dockerfile
## Build stage: pinned node image, cache and non-root build steps
ARG NODE_VERSION=25
FROM node:${NODE_VERSION}-alpine AS build

# Build-time args (can be overridden in CI/CD)
ARG VITE_API_BASE_URL=/api
ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}

WORKDIR /app

# Install dependencies using npm ci for reproducible builds
# Use package-lock.json if present; copy package files first to leverage cache
COPY package*.json ./
RUN npm ci --silent

# Copy source and build
COPY . .

RUN npm run build


## Runtime stage: lightweight nginx, serve static files securely
FROM nginx:1.27-alpine AS runtime

# Create directory for optional SSL certs
RUN mkdir -p /etc/nginx/ssl

# Install curl for healthchecks and small runtime tools
RUN apk add --no-cache curl

# Copy built assets
COPY --from=build /app/dist /usr/share/nginx/html

# Overwrite default config with our production nginx config
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Ensure correct ownership (nginx user) and permissions
RUN chown -R nginx:nginx /usr/share/nginx/html && \
		chmod -R 755 /usr/share/nginx/html

EXPOSE 80 443

# Simple healthcheck to let orchestrators know app is ready
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
	CMD curl -f http://localhost/ || exit 1

USER nginx

CMD ["nginx", "-g", "daemon off;"]


