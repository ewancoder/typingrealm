FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["TypingRealm.ApiHost/TypingRealm.ApiHost.csproj", "TypingRealm.ApiHost/"]
COPY ["TypingRealm.Framework/TypingRealm.Framework.csproj", "TypingRealm.Framework/"]
COPY ["TypingRealm.Typing.Api/TypingRealm.Typing.Api.csproj", "TypingRealm.Typing.Api/"]
COPY ["TypingRealm.Typing.DataAccess/TypingRealm.Typing.DataAccess.csproj", "TypingRealm.Typing.DataAccess/"]
COPY ["TypingRealm.Typing/TypingRealm.Typing.csproj", "TypingRealm.Typing/"]
RUN dotnet restore "./TypingRealm.Typing.Api/TypingRealm.Typing.Api.csproj"
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
