#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
RUN apt-get update \
 && apt-get install -y procps docker.io

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["xServer.D/xServerD.csproj", "xServer.D/"]
RUN dotnet restore "xServer.D/xServerD.csproj"
COPY . .
WORKDIR "/src/xServer.D"
RUN dotnet build "xServerD.csproj" -r linux-x64 -c Release -o /app/build  -p:ImportByWildcardBeforeSolution=false

FROM build AS publish
RUN dotnet publish "xServerD.csproj" -c Release  -r linux-x64 -v m  -o /app/publish /p:ImportByWildcardBeforeSolution=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "x42.xServerD.dll"]