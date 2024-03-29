FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["TypingRealm.Authentication.Core/TypingRealm.Authentication.Core.csproj", "TypingRealm.Authentication.Core/"]
COPY ["TypingRealm.Authentication/TypingRealm.Authentication.csproj", "TypingRealm.Authentication/"]
COPY ["TypingRealm.Common/TypingRealm.Common.csproj", "TypingRealm.Common/"]
COPY ["TypingRealm.Communication.Redis/TypingRealm.Communication.Redis.csproj", "TypingRealm.Communication.Redis/"]
COPY ["TypingRealm.Communication/TypingRealm.Communication.csproj", "TypingRealm.Communication/"]
COPY ["TypingRealm.Configuration/TypingRealm.Configuration.csproj", "TypingRealm.Configuration/"]
COPY ["TypingRealm.Data.Api/TypingRealm.Data.Api.csproj", "TypingRealm.Data.Api/"]
COPY ["TypingRealm.Data.Infrastructure/TypingRealm.Data.Infrastructure.csproj", "TypingRealm.Data.Infrastructure/"]
COPY ["TypingRealm.Data.Resources/TypingRealm.Data.Resources.csproj", "TypingRealm.Data.Resources/"]
COPY ["TypingRealm.DataAccess.Postgres/TypingRealm.DataAccess.Postgres.csproj", "TypingRealm.DataAccess.Postgres/"]
COPY ["TypingRealm.Hosting/TypingRealm.Hosting.csproj", "TypingRealm.Hosting/"]
COPY ["TypingRealm.Logging/TypingRealm.Logging.csproj", "TypingRealm.Logging/"]
COPY ["TypingRealm.Profiles.Api.Client/TypingRealm.Profiles.Api.Client.csproj", "TypingRealm.Profiles.Api.Client/"]
COPY ["TypingRealm.Profiles.Api.Resources/TypingRealm.Profiles.Api.Resources.csproj", "TypingRealm.Profiles.Api.Resources/"]
COPY ["TypingRealm.Profiles.Core/TypingRealm.Profiles.Core.csproj", "TypingRealm.Profiles.Core/"]
COPY ["TypingRealm.Serialization/TypingRealm.Serialization.csproj", "TypingRealm.Serialization/"]
COPY ["TypingRealm.TextProcessing/TypingRealm.TextProcessing.csproj", "TypingRealm.TextProcessing/"]
COPY ["TypingRealm.Texts.Api.Client/TypingRealm.Texts.Api.Client.csproj", "TypingRealm.Texts.Api.Client/"]
COPY ["TypingRealm.Texts.Core/TypingRealm.Texts.Core.csproj", "TypingRealm.Texts.Core/"]
RUN dotnet restore "TypingRealm.Data.Api/TypingRealm.Data.Api.csproj"
COPY . .
WORKDIR "/src/TypingRealm.Data.Api"
RUN dotnet build "TypingRealm.Data.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TypingRealm.Data.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TypingRealm.Data.Api.dll"]