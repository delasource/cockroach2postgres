# Use the official .NET 8 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy the project file and restore dependencies
COPY */*.csproj ./
RUN dotnet restore

# Copy the rest of the application source code
COPY . ./
RUN dotnet publish . -r linux-x64 -o ./dist


# Use the runtime-only image for the final stage
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app

# Copy the published application from the build stage
COPY --from=build /app/dist .

# Set the entry point for the application
RUN chmod +x cockroach2postgres
ENTRYPOINT ["./cockroach2postgres"]
