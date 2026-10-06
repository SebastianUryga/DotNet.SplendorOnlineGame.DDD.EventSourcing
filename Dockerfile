FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish Splendor.Api/Splendor.Api.csproj -c Release -o /app/api
RUN dotnet publish Splendor.BotWorker/Splendor.BotWorker.csproj -c Release -o /app/bot

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:10000
ENV Api__BaseUrl=http://localhost:10000
# ponytail: bot and API share one container (free tier); split into two services if memory or restarts become a problem
ENTRYPOINT ["sh", "-c", "(cd /app/bot && dotnet Splendor.BotWorker.dll) & cd /app/api && exec dotnet Splendor.Api.dll"]
