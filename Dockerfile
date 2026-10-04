ARG APP_BASE_PATH=/
FROM node:24-alpine AS frontend
ARG APP_BASE_PATH
ENV VITE_BASE_PATH=${APP_BASE_PATH}
WORKDIR /src/frontend
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY frontend/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend
WORKDIR /src
COPY backend/Monopoly.Server/Monopoly.Server.csproj backend/Monopoly.Server/
RUN dotnet restore backend/Monopoly.Server/Monopoly.Server.csproj
COPY backend/ backend/
RUN dotnet publish backend/Monopoly.Server/Monopoly.Server.csproj -c Release -o /out --no-restore
COPY --from=frontend /src/frontend/dist/ /out/wwwroot/

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
ARG APP_BASE_PATH
ENV APP_BASE_PATH=${APP_BASE_PATH}
WORKDIR /app
COPY --from=backend /out/ ./
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:5080
EXPOSE 5080
USER $APP_UID
ENTRYPOINT ["dotnet", "Monopoly.Server.dll"]
