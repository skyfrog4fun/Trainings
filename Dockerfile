# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Trainings.Web/Trainings.Web.csproj", "src/Trainings.Web/"]
COPY ["src/Trainings.Application/Trainings.Application.csproj", "src/Trainings.Application/"]
COPY ["src/Trainings.Domain/Trainings.Domain.csproj", "src/Trainings.Domain/"]
COPY ["src/Trainings.Infrastructure/Trainings.Infrastructure.csproj", "src/Trainings.Infrastructure/"]
RUN dotnet restore "src/Trainings.Web/Trainings.Web.csproj"

COPY . .
RUN dotnet publish "src/Trainings.Web/Trainings.Web.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

RUN mkdir -p /app/data && \
    chown -R app:app /app

USER app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Trainings.Web.dll"]
