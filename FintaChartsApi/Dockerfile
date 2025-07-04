# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)

#FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
#USER $APP_UID
#WORKDIR /app
#EXPOSE 8080
#EXPOSE 8081


# This stage is used to build the service project
#FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
#ARG BUILD_CONFIGURATION=Release
#WORKDIR /src
#COPY ["FintaChartsApi/FintaChartsApi.csproj", "FintaChartsApi/"]
#RUN dotnet restore "./FintaChartsApi/FintaChartsApi.csproj"
#COPY . .
#WORKDIR "/src/FintaChartsApi"
#RUN dotnet build "./FintaChartsApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
#FROM build AS publish
#ARG BUILD_CONFIGURATION=Release
#RUN dotnet publish "./FintaChartsApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
#FROM base AS final
#WORKDIR /app
#COPY --from=publish /app/publish .
#ENTRYPOINT ["dotnet", "FintaChartsApi.dll"]

# FintaChartsApi/Dockerfile (ФІНАЛЬНИЙ ВИГЛЯД)

# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.



# Цей етап використовується при запуску з VS у швидкому режимі (за замовчуванням для Debug конфігурації)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 80
EXPOSE 443

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["FintaChartsApi.csproj", "."]
RUN dotnet restore "FintaChartsApi.csproj"
COPY . .
RUN dotnet build "FintaChartsApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "FintaChartsApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This final stage creates the runnable image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
#ENTRYPOINT ["dotnet", "FintaChartsApi.dll"]
ENTRYPOINT ["dotnet", "FintaChartsApi.dll", "--urls", "http://0.0.0.0:80"]
