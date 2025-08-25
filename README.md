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

## Limitations

- Stores data in memory only (lost on restart).
- Diff works on .NET string chars, not byte-level.
- Only handles equal-length diffs; no insert/delete detection.
- No auth, no rate-limit, no payload size cap (demo only).

## Possible improvements
- Use DB for persistence instead of in-memory.
- Add DELETE /v1/diff/{id} to clean stored pairs.
- Support byte-level or grapheme-aware diff.
- Add Swagger/OpenAPI docs for testing.
- Add auth, rate-limit, and payload caps for prod use.