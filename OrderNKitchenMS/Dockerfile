# ============================================================================
# Stage 1: Build the Angular Application
# ============================================================================
FROM node:20-alpine AS build
WORKDIR /app

# Install dependencies using clean install
COPY package*.json ./
RUN npm ci

# Copy application source code and build
COPY . .
RUN npm run build -- --configuration production

# ============================================================================
# Stage 2: Serve using Nginx Web Server
# ============================================================================
FROM nginx:stable-alpine

# Copy build artifacts to the default Nginx web root directory
COPY --from=build /app/dist/OrderNKitchenMS/browser /usr/share/nginx/html

# Copy custom Nginx configuration to support SPA routing fallback and gzip
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
