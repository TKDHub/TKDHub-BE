# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["TKDHub.sln", "./"]
COPY ["Shared.Domain/Shared.Domain.csproj", "Shared.Domain/"]
COPY ["Shared.Application/Shared.Application.csproj", "Shared.Application/"]
COPY ["Shared.Infrastructure/Shared.Infrastructure.csproj", "Shared.Infrastructure/"]
COPY ["Shared.Contracts/Shared.Contracts.csproj", "Shared.Contracts/"]
COPY ["Identity.Domain/Identity.Domain.csproj", "Identity.Domain/"]
COPY ["Identity.Application/Identity.Application.csproj", "Identity.Application/"]
COPY ["Identity.Infrastructure/Identity.Infrastructure.csproj", "Identity.Infrastructure/"]
COPY ["Identity.API/Identity.API.csproj", "Identity.API/"]

# Restore dependencies
RUN dotnet restore "Identity.API/Identity.API.csproj"

# Copy all source files
COPY . .

# Build the application
WORKDIR "/src/Identity.API"
RUN dotnet build "Identity.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Identity.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Identity.API.dll"]
