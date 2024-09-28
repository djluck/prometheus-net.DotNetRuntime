#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["examples/AspNetCoreExample/AspNetCoreExample.csproj", "examples/AspNetCoreExample/"]
COPY ["src/prometheus-net.DotNetRuntime/prometheus-net.DotNetRuntime.csproj", "src/prometheus-net.DotNetRuntime/"]
RUN dotnet restore "examples/AspNetCoreExample/AspNetCoreExample.csproj"
COPY . .
WORKDIR "/src/examples/AspNetCoreExample"
RUN dotnet build "AspNetCoreExample.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AspNetCoreExample.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AspNetCoreExample.dll"]