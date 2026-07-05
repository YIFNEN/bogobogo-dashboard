# Security and Exclusion Policy

This public portfolio repository is intentionally smaller than the original working project.

## Excluded Secrets

The following must not be committed:

- `.env` files.
- OpenAI API keys.
- Supabase anon or service-role keys.
- Database URLs and passwords.
- JWTs, tokens, certificates, private keys.
- Local absolute paths that reveal private infrastructure.

## Excluded Large or Licensed Assets

The following are excluded from the public repository:

- Unity Asset Store package folders and `.unitypackage` files.
- Full Unity scene folders, generated `.meta` churn, Library, Temp, Logs, UserSettings.
- Windows/macOS executable builds.
- WebGL `Build`, `TemplateData`, `.wasm`, `.data`, and bundle outputs.
- Raw incident videos, thumbnails from private evidence, and uploaded files.
- Revit/RVT, IFC, FBX, NWD, point-cloud, and other CAD/BIM source files.

## Why Exclude Them

These files are excluded for four reasons:

1. They may contain credentials or environment-specific configuration.
2. They may be too large for a readable portfolio repository.
3. They may be licensed assets that should not be redistributed.
4. They may contain private team data or demo evidence.

## What Is Safe to Show

Safe materials include:

- Source excerpts written for the project.
- Architecture notes and implementation decisions.
- Sanitized screenshots.
- Verification scripts without secrets.
- README documentation explaining the role of each component.

## Pre-Push Checklist

Before pushing:

```powershell
rg -n "sk-|OPENAI|SUPABASE|SERVICE_ROLE|DATABASE_URL|password|secret|token|D:\\|C:\\" .
git status --short
```

Any real secret or local-only path found by this scan should be removed or replaced before publishing.
