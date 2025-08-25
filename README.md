# Pangea Diff API

Tiny .NET API that diffs two base64-encoded JSON inputs.

## Run
```bash
dotnet run --project src/DiffApi/DiffApi.csproj --urls http://localhost:5000
```

## Endpoints
- POST /v1/diff/{id}/left
- POST /v1/diff/{id}/right
- GET  /v1/diff/{id}

## Body format
Send body as JSON string with base64 inside. Base64 decodes to `{"input":"..."}`.

### PowerShell ex
```powershell
$payload = '{"input":"testValue"}'
$base64  = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($payload))
$body    = '"' + $base64 + '"'

Invoke-RestMethod -Method Post -Uri "http://localhost:5000/v1/diff/123/left" -ContentType "application/custom" -Body $body
Invoke-RestMethod -Method Post -Uri "http://localhost:5000/v1/diff/123/right" -ContentType "application/custom" -Body $body
Invoke-RestMethod -Method Get  -Uri "http://localhost:5000/v1/diff/123"
```