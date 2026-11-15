# =============================================================================
# CineSeat Web - Multi-stage Dockerfile (.NET 8 ASP.NET Core MVC)
# =============================================================================

# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore NuGet dependencies
COPY ["src/CineSeat.Web/CineSeat.Web.csproj", "src/CineSeat.Web/"]
RUN dotnet restore "src/CineSeat.Web/CineSeat.Web.csproj"

# Copy source code and build Release
COPY ["src/CineSeat.Web/", "src/CineSeat.Web/"]
WORKDIR "/src/src/CineSeat.Web"
RUN dotnet publish "CineSeat.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime image (chỉ chứa runtime nhẹ ~200MB)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV NLS_LANG=AMERICAN_AMERICA.AL32UTF8
ENV LANG=C.UTF-8

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CineSeat.Web.dll"]
