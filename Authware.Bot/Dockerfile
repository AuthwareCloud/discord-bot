FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Authware.Bot/Authware.Bot.csproj", "Authware.Bot/"]
COPY ["Authware.Bot.Commands/Authware.Bot.Commands.csproj", "Authware.Bot.Commands/"]
COPY ["Authware.Bot.Common/Authware.Bot.Common.csproj", "Authware.Bot.Common/"]
COPY ["Authware.Bot.Services/Authware.Bot.Services.csproj", "Authware.Bot.Services/"]
COPY ["Authware.Bot.Shared/Authware.Bot.Shared.csproj", "Authware.Bot.Shared/"]
RUN dotnet restore "Authware.Bot/Authware.Bot.csproj"
COPY . .
WORKDIR "/src/Authware.Bot"
RUN dotnet build "Authware.Bot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Authware.Bot.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Authware.Bot.dll"]
