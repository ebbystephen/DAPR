# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# --- Stage 1: Base Runtime Setup ---
# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
# Ensure the application runs as a non-root user (security best practice)
USER $APP_UID
WORKDIR /app
# Expose the standard ASP.NET Core ports.
EXPOSE 8080
EXPOSE 8081

# --- Stage 2: Build Application ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Dapr.Web.csproj", "."]
RUN dotnet restore "./Dapr.Web.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./Dapr.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

# --- Stage 3: Publish Application ---
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Dapr.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# --- Stage 4: Final Production/Dapr Image ---
# This is the final application image.
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# CRITICAL for DAPR: The Dapr sidecar uses --app-port to communicate with your app.
# We set ASPNETCORE_URLS to listen on this port (e.g., 5213).
ENV ASPNETCORE_URLS=http://+:5213
EXPOSE 5213 

# Define the entry point for the application
# When running with 'dapr run', this is the command executed by the sidecar.
ENTRYPOINT ["dotnet", "Dapr.Web.dll"]