# DNA Project - Math Operations API

A .NET 10 REST API that performs arithmetic operations with JWT authentication, caching, Mockoon integration, and Kafka event publishing.

## Project Structure

```
Matrix mizrachi/          - Main API project
Matrix mizrachi.Tests/    - XUnit test project
swagger.yaml              - OpenAPI specification
```

## JWT Secret Configuration

The JWT secret is defined in `appsettings.json` under `Jwt:Secret`. It must be at least 32 characters long.

```json
"Jwt": {
  "Secret": "super-secret-jwt-key-minimum-32-chars!!",
  "Issuer": "matrix-mizrachi",
  "Audience": "matrix-mizrachi-client"
}
```

**For production**, override via environment variable or user secrets:
```bash
dotnet user-secrets set "Jwt:Secret" "your-production-secret-32-chars-min"
```

To get a token for testing, call:
```
POST /api/auth/token
```

Then use the returned token in the `Authorization: Bearer {token}` header.

## Mockoon Setup

1. Download and install Mockoon from https://mockoon.com
2. Open Mockoon and create a new environment on port **3001**
3. Add a route:
   - Method: `GET`
   - Path: `/api/meta/:operation`
   - Response body (JSON):
     ```json
     {
       "operation": "{{urlParam 'operation'}}",
       "description": "Performs {{urlParam 'operation'}} arithmetic"
     }
     ```
4. Start the Mockoon environment

The API is configured to call `http://localhost:3001` (see `Mockoon:BaseUrl` in appsettings).

## Running the API

```bash
cd "Matrix mizrachi"
dotnet run
```

Swagger UI is available at: http://localhost:5000/swagger

## Running Tests

```bash
# Add the test project to the solution first (in Visual Studio or CLI):
dotnet sln add "Matrix mizrachi.Tests/Matrix mizrachi.Tests.csproj"

# Run all tests
dotnet test
```

## Kafka (Bonus)

Kafka is disabled by default (`Kafka:Enabled: false`). To enable:

1. Start Kafka (see Docker section below, or run locally on port 9092)
2. Set in `appsettings.json`:
   ```json
   "Kafka": {
     "Enabled": true,
     "BootstrapServers": "localhost:9092"
   }
   ```

On every cache miss, an event is published to topic `math-operations` with:
```json
{
  "requestId": "...",
  "operation": "add",
  "x": 10,
  "y": 5,
  "result": 15,
  "timestamp": "2024-01-01T00:00:00Z"
}
```

## Docker (Bonus)

Build and run everything with Docker Compose:

```bash
docker-compose up --build
```

Services started:
- **API** on port 5000
- **Kafka** on port 9092
- **Zookeeper** on port 2181
- **Redis** on port 6379

> Note: Mockoon is run separately (see Mockoon Setup above). Export your Mockoon environment and mount it in the compose file if needed.

## API Reference

### POST /api/math

**Headers:**
- `Authorization: Bearer {jwt_token}` (required)
- `X-ArithmeticOp-ID: add|subtract|multiply|divide` (required)

**Body:**
```json
{
  "operation": "add",
  "x": 10,
  "y": 5
}
```

**Response 200:**
```json
{
  "result": 15,
  "description": "Performs add arithmetic",
  "fromCache": false
}
```

**Response 400** (division by zero):
```json
{ "error": "Cannot divide by zero." }
```

**Response 401**: Missing or invalid JWT token

### POST /api/auth/token

Returns a JWT token for testing (1 hour expiry).

**Response 200:**
```json
{ "token": "eyJ..." }
```
