FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

RUN mkdir -p /logs

EXPOSE 9000

ENV LOG_SERVER_PORT=9000
ENV LOG_DIR=/logs

ENTRYPOINT ["dotnet", "LogServer.dll"]
