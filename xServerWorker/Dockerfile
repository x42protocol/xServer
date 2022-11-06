#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

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