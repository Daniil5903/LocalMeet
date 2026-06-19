FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["LocalMeet/LocalMeet.csproj", "LocalMeet/"]
RUN dotnet restore "LocalMeet/LocalMeet.csproj"

COPY . .
WORKDIR /src/LocalMeet

RUN dotnet publish "LocalMeet.csproj" --configuration Release --output /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORTS=8080
ENV DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "LocalMeet.dll"]