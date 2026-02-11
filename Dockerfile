# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["MentorX.API/MentorX.API.csproj", "MentorX.API/"]
COPY ["MentorX.Application/MentorX.Application.csproj", "MentorX.Application/"]
COPY ["MentorX.Domain/MentorX.Domain.csproj", "MentorX.Domain/"]
COPY ["MentorX.Infrastructure/MentorX.Infrastructure.csproj", "MentorX.Infrastructure/"]

RUN dotnet restore "MentorX.API/MentorX.API.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/MentorX.API"
RUN dotnet build "MentorX.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "MentorX.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create logs directory
RUN mkdir -p /app/logs

# Copy published app
COPY --from=publish /app/publish .

# Cloud Run uses PORT environment variable (defaults to 8080)
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "MentorX.API.dll"]
