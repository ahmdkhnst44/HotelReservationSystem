# 1. مرحلة البناء
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["HotelReservationSystem.csproj", "./"]
RUN dotnet restore "HotelReservationSystem.csproj"

COPY . .
RUN dotnet publish "HotelReservationSystem.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. مرحلة التشغيل
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "HotelReservationSystem.dll"]