FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY ["src/IndustrialPlatform.Shared/IndustrialPlatform.Shared.csproj", "src/IndustrialPlatform.Shared/"]
COPY ["src/IndustrialPlatform.Domain/IndustrialPlatform.Domain.csproj", "src/IndustrialPlatform.Domain/"]
COPY ["src/IndustrialPlatform.Application/IndustrialPlatform.Application.csproj", "src/IndustrialPlatform.Application/"]
COPY ["src/IndustrialPlatform.Identity/IndustrialPlatform.Identity.csproj", "src/IndustrialPlatform.Identity/"]
COPY ["src/IndustrialPlatform.Persistence/IndustrialPlatform.Persistence.csproj", "src/IndustrialPlatform.Persistence/"]
COPY ["src/IndustrialPlatform.Infrastructure/IndustrialPlatform.Infrastructure.csproj", "src/IndustrialPlatform.Infrastructure/"]
COPY ["src/IndustrialPlatform.Api/IndustrialPlatform.Api.csproj", "src/IndustrialPlatform.Api/"]
RUN dotnet restore "src/IndustrialPlatform.Api/IndustrialPlatform.Api.csproj"

COPY . .
WORKDIR "/src/src/IndustrialPlatform.Api"
RUN dotnet publish "IndustrialPlatform.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "IndustrialPlatform.Api.dll"]
