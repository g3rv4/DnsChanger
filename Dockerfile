FROM mcr.microsoft.com/dotnet/core/sdk:3.1.404-alpine3.12 AS build
COPY . /var/src
RUN dotnet publish -c Release /var/src/DnsChanger.Web/DnsChanger.Web.csproj -o /var/src/build

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1.10-alpine3.12
COPY --from=build /var/src/build /var/app
WORKDIR /var/app
CMD ["dotnet", "DnsChanger.Web.dll"]