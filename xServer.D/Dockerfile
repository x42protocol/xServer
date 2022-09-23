#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
RUN apt-get update \
 && apt-get install -y procps

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY [".", "."]
RUN dotnet restore "xServer.D/xServerD.csproj"
COPY . .
RUN dotnet build -c Release -r linux-x64 -v m xServer.D.sln -p:ImportByWildcardBeforeSolution=false

FROM build AS publish
RUN dotnet publish -c Release -r linux-x64 -v m -o ./build xServer.D/xServerD.csproj -p:ImportByWildcardBeforeSolution=false

FROM build AS debug
WORKDIR /app

RUN apt-get update \
 && apt-get install -y unzip

RUN curl -sSL https://aka.ms/getvsdbgsh | /bin/sh /dev/stdin -v latest -l ~/vsdbg

RUN dotnet publish -c Debug -r linux-x64 -v m -o ./build xServer.D/xServerD.csproj -p:ImportByWildcardBeforeSolution=false


ENTRYPOINT ["dotnet", "x42.xServerD.dll"]

FROM base AS final
WORKDIR /app

COPY --from=publish /src/build/xserver.d .
ENTRYPOINT ["dotnet", "x42.xServerD.dll"]