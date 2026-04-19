FROM node:20 AS build
WORKDIR /app
COPY package.json package-lock.json* ./
RUN npm install
COPY . .
RUN npm run build

FROM node:20-slim AS runtime
WORKDIR /app
COPY --from=build /app/dist/RentGrid.Web /app/dist
COPY --from=build /app/node_modules /app/node_modules
ENV PORT=80
EXPOSE 80
CMD ["node", "dist/server/server.mjs"]
