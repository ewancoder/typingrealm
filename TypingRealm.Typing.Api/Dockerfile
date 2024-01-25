FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TypingRealm.Typing.Api/TypingRealm.Typing.Api.csproj", "TypingRealm.Typing.Api/"]
RUN dotnet restore "./TypingRealm.Typing.Api/./TypingRealm.Typing.Api.csproj"
COPY . .
WORKDIR "/src/TypingRealm.Typing.Api"
RUN dotnet build "./TypingRealm.Typing.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TypingRealm.Typing.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TypingRealm.Typing.Api.dll"]
