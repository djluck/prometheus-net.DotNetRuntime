# FROM mcr.microsoft.com/dotnet/core/sdk:3.1.201 AS build
FROM mcr.microsoft.com/dotnet/core/sdk:3.1.406 AS build
#FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish "examples/AspNetCoreExample" -c Release -o /app

#FROM mcr.microsoft.com/dotnet/core/aspnet:3.1.3 AS final
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1.10 AS final
#FROM mcr.microsoft.com/dotnet/aspnet:5.0 as final
WORKDIR /app
COPY --from=build /app /app
ENTRYPOINT ["dotnet", "AspNetCoreExample.dll"]
