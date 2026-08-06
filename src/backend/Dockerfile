FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY PTKD.Domain/PTKD.Domain.csproj PTKD.Domain/
COPY PTKD.Application/PTKD.Application.csproj PTKD.Application/
COPY PTKD.Infrastructure/PTKD.Infrastructure.csproj PTKD.Infrastructure/
COPY PTKD.Api/PTKD.Api.csproj PTKD.Api/
COPY PTKD.DbMigrator/PTKD.DbMigrator.csproj PTKD.DbMigrator/
COPY PTKD.Bootstrap/PTKD.Bootstrap.csproj PTKD.Bootstrap/
COPY PTKD.Worker/PTKD.Worker.csproj PTKD.Worker/
RUN dotnet restore PTKD.Api/PTKD.Api.csproj

COPY . .
RUN dotnet publish PTKD.Api/PTKD.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "PTKD.Api.dll"]
