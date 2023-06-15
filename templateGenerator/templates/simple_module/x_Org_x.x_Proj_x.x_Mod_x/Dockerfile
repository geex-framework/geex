#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["x_Org_x.x_Proj_x.x_Mod_x/x_Org_x.x_Proj_x.x_Mod_x.csproj", "x_Org_x.x_Proj_x.x_Mod_x/"]
RUN dotnet restore "x_Org_x.x_Proj_x.x_Mod_x/x_Org_x.x_Proj_x.x_Mod_x.csproj"
COPY . .
WORKDIR "/src/x_Org_x.x_Proj_x.x_Mod_x"
RUN dotnet build "x_Org_x.x_Proj_x.x_Mod_x.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "x_Org_x.x_Proj_x.x_Mod_x.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "x_Org_x.x_Proj_x.x_Mod_x.dll"]
