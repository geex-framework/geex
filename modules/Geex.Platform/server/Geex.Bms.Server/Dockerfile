#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src
COPY ["Geex.Server/Geex.Server.csproj", "Geex.Server/"]
COPY ["Geex.Core/Geex.Core.csproj", "Geex.Core/"]
COPY ["Geex.Shared/Geex.Shared.csproj", "Geex.Shared/"]
COPY [".submodules/MongoDB.Entities/MongoDB.Entities/MongoDB.Entities.csproj", ".submodules/MongoDB.Entities/MongoDB.Entities/"]
COPY ["Geex.Data/Geex.Data.csproj", "Geex.Data/"]
RUN dotnet restore "Geex.Server/Geex.Server.csproj"
COPY . .
WORKDIR "/src/Geex.Server"
RUN dotnet build "Geex.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Geex.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Geex.Server.dll"]
