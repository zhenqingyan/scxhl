# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY *.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create non-root user
RUN adduser -u 1000 --disabled-password appuser && chown -R appuser /app
USER appuser

EXPOSE 8080

COPY --from=build /app .

ENTRYPOINT ["dotnet", "henglong.Web.dll"]
