FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY OrderNKitchenMS-API/*.csproj OrderNKitchenMS-API/
RUN dotnet restore OrderNKitchenMS-API/OrderNKitchenMS-API.csproj

COPY . .
WORKDIR /src/OrderNKitchenMS-API
RUN dotnet publish \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet","OrderNKitchenMS-API.dll"]