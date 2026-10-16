# Compilación de Angular.
FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

# El servidor generado por Angular incluye sus dependencias de ejecución.
FROM node:22-alpine AS runtime
WORKDIR /app
ENV NODE_ENV=production
ENV PORT=4000
COPY --from=build --chown=node:node /app/dist/optica-web ./dist/optica-web
USER node
EXPOSE 4000
CMD ["node", "dist/optica-web/server/server.mjs"]
