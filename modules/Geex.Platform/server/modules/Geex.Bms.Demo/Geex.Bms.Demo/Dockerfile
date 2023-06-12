#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Geex.Bms.Demo/Geex.Bms.Demo.csproj", "Geex.Bms.Demo/"]
RUN dotnet restore "Geex.Bms.Demo/Geex.Bms.Demo.csproj"
COPY . .
WORKDIR "/src/Geex.Bms.Demo"
RUN dotnet build "Geex.Bms.Demo.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Geex.Bms.Demo.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Geex.Bms.Demo.dll"]
