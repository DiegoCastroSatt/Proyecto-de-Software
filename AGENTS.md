# Project agent instructions

## Project structure

- `Optica.Api/` contains the .NET API. Keep database mappings in `Data/` and domain code in `Modules/`.
- `Optica_front/` contains the Angular application.
- `database/schema.sql` is the database bootstrap script.
- `docker-compose.yml` orchestrates the API and MySQL services.

## Safety rules

- Do not edit generated folders such as `bin/`, `obj/`, `node_modules/`, or `dist/`.
- Do not add, read aloud, or commit secrets. Use the ignored `.env` file and update only `.env.example` with placeholders.
- Preserve the existing Spanish database table and column names unless a database migration is included in the change.
- Review generated code, the diff, and relevant build checks before committing.

## Naming convention

- Use English for source-code identifiers, file names, API route names, and technical comments.
- Use PascalCase for C# types and public members; use camelCase for C# local variables and TypeScript members.
- Keep user-facing Spanish text in the current product language.
