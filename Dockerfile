FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 5001
ENV ASPNETCORE_URLS=http://+:5000

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Matrix mizrachi/Matrix mizrachi.csproj", "Matrix mizrachi/"]
RUN dotnet restore "Matrix mizrachi/Matrix mizrachi.csproj"

COPY . .
WORKDIR "/src/Matrix mizrachi"
RUN dotnet build "Matrix mizrachi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Matrix mizrachi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Matrix mizrachi.dll"]
