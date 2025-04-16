FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "UserService.dll"]


# # Build stage
# FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
# WORKDIR /src

# COPY ["UserService.csproj", "./"]
# RUN dotnet restore "./UserService.csproj"

# COPY . .
# RUN dotnet publish "./UserService.csproj" -c Release -o /app/publish

# # Runtime stage
# FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
# WORKDIR /app
# EXPOSE 80

# COPY --from=build /app/publish .

# ENTRYPOINT ["dotnet", "UserService.dll"]
