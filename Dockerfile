FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 7170

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CarInsuranceBot.csproj", "."]
RUN dotnet restore "CarInsuranceBot.csproj"
COPY . .
RUN dotnet build "CarInsuranceBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CarInsuranceBot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CarInsuranceBot.dll"]