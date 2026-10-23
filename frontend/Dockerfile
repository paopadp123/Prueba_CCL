# Imagen del front: compila Angular y lo sirve con nginx, que además reenvía /api a la API.

# ---- Compilación ----
FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npx ng build

# ---- Servidor ----
FROM nginx:1.27-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/inventory-web/browser /usr/share/nginx/html
EXPOSE 80
