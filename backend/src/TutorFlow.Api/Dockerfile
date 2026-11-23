FROM mcr.microsoft.com/dotnet/sdk:11.0@sha256:65f3c7c6a25a239ab3e6719f2896147e7b68242ff2d8818dd70498bde6ef8bea AS build
WORKDIR /src/backend
COPY backend/global.json backend/Directory.Build.props backend/Directory.Packages.props backend/CodeMetricsConfig.txt backend/.editorconfig ./
COPY backend/src/TutorFlow.Core/TutorFlow.Core.csproj backend/src/TutorFlow.Core/packages.lock.json src/TutorFlow.Core/
COPY backend/src/TutorFlow.Infrastructure/TutorFlow.Infrastructure.csproj backend/src/TutorFlow.Infrastructure/packages.lock.json src/TutorFlow.Infrastructure/
COPY backend/src/TutorFlow.Api/TutorFlow.Api.csproj backend/src/TutorFlow.Api/packages.lock.json src/TutorFlow.Api/
RUN dotnet restore src/TutorFlow.Api/TutorFlow.Api.csproj --locked-mode
COPY backend/src ./src
RUN dotnet publish src/TutorFlow.Api/TutorFlow.Api.csproj -c Release --no-restore -o /out

FROM mcr.microsoft.com/dotnet/aspnet:11.0@sha256:bc0d6653979efba491ab3d6f589d691f96c309c3c3ef81325efa6978e7b7f242 AS final
ENV ASPNETCORE_HTTP_PORTS=8080
WORKDIR /app
COPY --from=build /out ./
COPY reglamento ./reglamento
RUN mkdir /app/auditoria && chown $APP_UID:$APP_UID /app/auditoria
USER $APP_UID
EXPOSE 8080
HEALTHCHECK --interval=10s --timeout=3s --start-period=20s --retries=3 \
  CMD ["bash", "-c", "exec 3<>/dev/tcp/127.0.0.1/8080 && printf 'GET /api/salud HTTP/1.0\r\nHost: localhost\r\n\r\n' >&3 && read -r linea <&3 && case \"$linea\" in *' 200 '*) exit 0;; esac; exit 1"]
ENTRYPOINT ["dotnet", "TutorFlow.Api.dll"]
