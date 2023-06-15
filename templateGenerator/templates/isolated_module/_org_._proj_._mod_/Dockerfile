#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["_org_._proj_._mod_/_org_._proj_._mod_.csproj", "_org_._proj_._mod_/"]
RUN dotnet restore "_org_._proj_._mod_/_org_._proj_._mod_.csproj"
COPY . .
WORKDIR "/src/_org_._proj_._mod_"
RUN dotnet build "_org_._proj_._mod_.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "_org_._proj_._mod_.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "_org_._proj_._mod_.dll"]
