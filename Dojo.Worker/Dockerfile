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
COPY ["Dojo.Worker/Dojo.Worker.csproj", "Dojo.Worker/"]

# Restore dependencies
RUN dotnet restore "Dojo.Worker/Dojo.Worker.csproj"

# Copy all source files
COPY . .

# Build the application
WORKDIR "/src/Dojo.Worker"
RUN dotnet build "Dojo.Worker.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Dojo.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage — no ASP.NET Core needed, this process never serves HTTP
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Dojo.Worker.dll"]
