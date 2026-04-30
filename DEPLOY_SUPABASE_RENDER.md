# Supabase Postgres + Render Deployment (Fast Path)

This project is ready for the setup: Supabase for PostgreSQL, Render for hosting the ASP.NET Core app.

## 1) Create Supabase database

1. Create a new Supabase project.
2. In Supabase dashboard, open Project Settings -> Database.
3. Copy the direct connection string (not localhost), then convert it to ASP.NET format:

```text
Host=<db-host>;Port=5432;Database=postgres;Username=postgres.<project-ref>;Password=<db-password>;SSL Mode=Require;Trust Server Certificate=true
```

Notes:
- Use direct DB host/port for EF Core migrations on startup.
- Do not commit this string in source control.

## 2) Deploy on Render using blueprint

1. Push this repository to GitHub.
2. In Render, choose New -> Blueprint.
3. Select the repository and deploy.
4. Render reads render.yaml from repo root and creates the web service.

## 3) Set required Render environment variables

Set these in the Render service before first successful boot:

- ConnectionStrings__Default: Supabase PostgreSQL connection string
- EmailSettings__PublicBaseUrl: your Render URL or custom HTTPS domain
- AttendanceQrSettings__SigningKey: long random secret value

Mail settings (required by current app config/binding):
- EmailSettings__Host
- EmailSettings__Port
- EmailSettings__Username
- EmailSettings__Password
- EmailSettings__FromAddress
- EmailSettings__FromName
- EmailSettings__UseSsl

Recommended production cookie override:
- CookieSettings__SecurePolicy=Always

## 4) First boot behavior

On startup the app automatically:
- Runs EF Core migrations.
- Runs seed data initialization.

If deployment fails, check Render logs first for:
- Invalid/missing EmailSettings__PublicBaseUrl
- Invalid DB connection string
- TLS/SSL DB connection settings

## 5) Verify deployment

After deploy, confirm:
- /health returns HTTP 200
- /login loads
- Signup/login email flows use the HTTPS public base URL

## 6) Optional hardening after first successful deploy

- Move to a custom domain and update EmailSettings__PublicBaseUrl.
- Rotate AttendanceQrSettings__SigningKey periodically.
- Restrict CORS/origins if you split frontend later.
- Add managed backups/alerts in Supabase and Render.
