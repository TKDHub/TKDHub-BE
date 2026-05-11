# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["TKDHub.sln", "./"]
COPY ["Shared.Domain/Shared.Domain.csproj", "Shared.Domain/"]
COPY ["Shared.Application/Shared.Application.csproj", "Shared.Application/"]
COPY ["Shared.Infrastructure/Shared.Infrastructure.csproj", "Shared.Infrastructure/"]
COPY ["Shared.Contracts/Shared.Contracts.csproj", "Shared.Contracts/"]
COPY ["Dojo.Domain/Dojo.Domain.csproj", "Dojo.Domain/"]
COPY ["Dojo.Application/Dojo.Application.csproj", "Dojo.Application/"]
COPY ["Dojo.Infrastructure/Dojo.Infrastructure.csproj", "Dojo.Infrastructure/"]
COPY ["Dojo.API/Dojo.API.csproj", "Dojo.API/"]

# Restore dependencies
RUN dotnet restore "Dojo.API/Dojo.API.csproj"

# Copy all source files
COPY . .

# Build the application
WORKDIR "/src/Dojo.API"
RUN dotnet build "Dojo.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Dojo.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Dojo.API.dll"]
