FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY LogServer.csproj .
COPY LogServer.cs     .

RUN dotnet restore
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app .

RUN mkdir -p /logs

EXPOSE 9000

ENV LOG_SERVER_PORT=9000
ENV LOG_DIR=/logs

ENTRYPOINT ["dotnet", "LogServer.dll"]
