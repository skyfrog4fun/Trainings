# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Trainings.Web/Trainings.Web.csproj", "src/Trainings.Web/"]
COPY ["src/Trainings.Application/Trainings.Application.csproj", "src/Trainings.Application/"]
COPY ["src/Trainings.Domain/Trainings.Domain.csproj", "src/Trainings.Domain/"]
COPY ["src/Trainings.Infrastructure/Trainings.Infrastructure.csproj", "src/Trainings.Infrastructure/"]
RUN dotnet restore "src/Trainings.Web/Trainings.Web.csproj"

COPY . .
# Do NOT pass --no-restore here. The earlier restore step ran without .razor files
# present, so MSBuild set RequiresAspNetWebAssets=false and skipped restoring
# Microsoft.AspNetCore.App.Internal.Assets (the package that provides
# wwwroot/_framework/blazor.web.js). Running restore again with the full source
# tree ensures that package is resolved and the Blazor JS files are included in
# the publish output.
RUN dotnet publish "src/Trainings.Web/Trainings.Web.csproj" \
    --configuration Release \
    --output /app/publish

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
