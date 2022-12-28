#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

RUN apt-get update \
 && apt-get install -y procps docker.io curl ca-certificates gnupg lsb-release

RUN curl -SL https://github.com/docker/compose/releases/download/v2.12.2/docker-compose-linux-x86_64 -o /usr/local/bin/docker-compose
RUN ["chmod", "+x", "/usr/local/bin/docker-compose"]

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["xServerWorker/xServerWorker.csproj", "xServerWorker/"]
COPY ["Common/Common.csproj", "Common/"]
COPY ["xServer.D/xServerD.csproj", "xServer.D/"]
RUN dotnet restore "xServerWorker/xServerWorker.csproj"
COPY . .
WORKDIR "/src/xServerWorker"
RUN dotnet build "xServerWorker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "xServerWorker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "xServerWorker.dll"]