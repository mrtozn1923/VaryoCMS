FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/VaryoCms.Web/VaryoCms.Web.csproj",             "src/VaryoCms.Web/"]
COPY ["src/VaryoCms.Application/VaryoCms.Application.csproj", "src/VaryoCms.Application/"]
COPY ["src/VaryoCms.Infrastructure/VaryoCms.Infrastructure.csproj", "src/VaryoCms.Infrastructure/"]
COPY ["src/VaryoCms.Domain/VaryoCms.Domain.csproj",        "src/VaryoCms.Domain/"]
RUN dotnet restore "src/VaryoCms.Web/VaryoCms.Web.csproj"
COPY . .
WORKDIR "/src/src/VaryoCms.Web"
RUN dotnet publish "VaryoCms.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "VaryoCms.Web.dll"]
