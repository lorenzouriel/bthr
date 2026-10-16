FROM node:24.11.1-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
ARG VITE_TELEMETRY_ENABLED=false
ARG VITE_TELEMETRY_ENVIRONMENT=Development
ARG VITE_TELEMETRY_INSTANCE=web-local
ARG VITE_SERVICE_VERSION=local
ENV VITE_API_BASE_URL=""
RUN npm run build
FROM nginx:1.28.0-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
HEALTHCHECK CMD wget -q -O /dev/null http://127.0.0.1/health || exit 1
