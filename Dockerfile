# syntax=docker/dockerfile:1

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

RUN addgroup --system --gid 1000 appgroup && \
    adduser --system --uid 1000 --ingroup appgroup appuser

COPY --from=build /app/publish .

RUN mkdir -p /app/data && \
    chown -R appuser:appgroup /app

USER appuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Trainings.Web.dll"]
