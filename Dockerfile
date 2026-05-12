# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Attendance_Management_System/Attendance_Management_System.slnx ./Attendance_Management_System/
COPY Attendance_Management_System/Attendance_Management_System/Attendance_Management_System.csproj ./Attendance_Management_System/Attendance_Management_System/
RUN dotnet restore ./Attendance_Management_System/Attendance_Management_System/Attendance_Management_System.csproj

COPY Attendance_Management_System/Attendance_Management_System ./Attendance_Management_System/Attendance_Management_System
WORKDIR /src/Attendance_Management_System/Attendance_Management_System
RUN dotnet publish Attendance_Management_System.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 8080

RUN apt-get update \
	&& apt-get install -y --no-install-recommends curl \
	&& rm -rf /var/lib/apt/lists/*

RUN useradd --create-home --uid 10001 appuser

COPY --from=build /app/publish .
RUN mkdir -p /app/data-protection-keys \
	&& chown -R appuser:appuser /app

USER appuser

HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 CMD curl --fail http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "Attendance_Management_System.dll"]
