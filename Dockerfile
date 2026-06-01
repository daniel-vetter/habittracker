# 1) Publish the backend and generate the NSwag TypeScript client from the API.
#    The generator (Program.cs) returns before any DB work, so no database is
#    needed; the placeholder connection string only satisfies eager config reads.
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS backend
WORKDIR /src
COPY src/backend/ .
RUN dotnet publish HabitTracker.WebApp/HabitTracker.WebApp.csproj -c Release -o /publish
ENV ConnectionStrings__db="Host=localhost;Database=placeholder"
RUN dotnet /publish/HabitTracker.WebApp.dll --generateTypeScriptClient /server.ts

# 2) Build the Angular frontend against the freshly generated server.ts.
FROM node:22-alpine AS frontend
WORKDIR /src
COPY src/frontend/package.json src/frontend/package-lock.json ./
RUN npm ci
COPY src/frontend/ .
COPY --from=backend /server.ts ./src/app/server.ts
RUN npm run build

# 3) Runtime image: backend serves the SPA from wwwroot.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app
COPY --from=backend /publish /app
COPY --from=frontend /src/dist/HabitTracker.Client/browser /app/wwwroot
ARG BUILD_VERSION
ARG BUILD_TIME
LABEL build.version=$BUILD_VERSION
ENV ASPNETCORE_HTTP_PORTS=80
ENV BUILD_COMMIT=$BUILD_VERSION
ENV BUILD_TIME=$BUILD_TIME
EXPOSE 80
ENTRYPOINT [ "dotnet", "HabitTracker.WebApp.dll" ]
