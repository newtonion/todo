FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY TodoService.sln ./
COPY Api/Api.csproj Api/
COPY Api.Tests/Api.Tests.csproj Api.Tests/
RUN dotnet restore Api/Api.csproj

COPY . .
WORKDIR /src/Api
RUN dotnet publish Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Api.dll"]
