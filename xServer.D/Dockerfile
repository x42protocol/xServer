#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
RUN apt-get update \
 && apt-get install -y procps docker.io curl ca-certificates gnupg lsb-release


RUN curl -SL https://github.com/docker/compose/releases/download/v2.12.2/docker-compose-linux-x86_64 -o /usr/local/bin/docker-compose
RUN ["chmod", "+x", "/usr/local/bin/docker-compose"]

#RUN mkdir -p /etc/apt/keyrings
#RUN curl -fsSL https://download.docker.com/linux/debian/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
#RUN echo \
#  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/debian \
#  $(lsb_release -cs) stable" | tee /etc/apt/sources.list.d/docker.list > /dev/null

#RUN apt-get update \
#	&& apt-get install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin

RUN echo 'alias docker-compose="docker compose"' >> ~/.bashrc


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
#CMD /bin/bash -c "source /root/.bashrc"
ENTRYPOINT ["dotnet", "x42.xServerD.dll"]