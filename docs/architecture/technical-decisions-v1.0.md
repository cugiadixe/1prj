# PTKD ERP - Technical Decisions v1.0

This document records the confirmed technical decisions for the PTKD ERP project.

## Architecture
- **Paradigm:** Modular Monolith.
- **Organization:** Vertical Slice by business feature.
- **Microservices:** Do not use microservices.
- **MediatR:** Do not add MediatR in the initial version.

## Backend
- **Framework:** ASP.NET Core Web API.
- **Routing:** Controller-based API. All public endpoints use the `/api/v2` prefix.
- **Contracts:** Use DTOs for requests and responses. Use ProblemDetails and stable business error codes for error handling.
- **Data Access:** 
  - EF Core for ordinary CRUD operations.
  - Dapper or stored procedures for complex/sensitive transactions including approval, payment, reconciliation, customer merge, and other sensitive operations.

## Frontend
- **Framework & Build:** React with TypeScript, built with Vite.
- **UI Component Library:** Ant Design.
- **State Management:** 
  - TanStack Query for server state.
  - Zustand only for necessary client state.
- **Forms & Validation:** React Hook Form and Zod.

## Database
- **Engine:** Microsoft SQL Server.
- **Environments:** Development database is `PTKD_DEV`. Test database is `PTKD_TEST`.
- **Migrations:** Versioned forward and rollback SQL scripts. No automatic production schema updates on startup.
- **Concurrency:** Use `rowversion` for optimistic concurrency control.

## Testing
- **Backend:** xUnit for test execution, NSubstitute for mocking, and WebApplicationFactory for API integration tests.
- **Frontend:** Vitest and React Testing Library for unit/component tests. Playwright for critical end-to-end scenarios.

## Logging and Audit
- **Technical Logs:** Use Serilog.
- **Business Audit:** Stored securely in SQL Server.
- **Separation:** Technical logs and business audit must remain strictly separate.
- **Immutability:** Business audit records must be append-only and immutable to normal users.

## Secrets Management
- **Backend:** .NET User Secrets for local development.
- **Frontend:** `.env.local` for local configuration.
- **Security:** Never commit secrets, passwords, or tokens to version control.

## Authentication
- **Initial Version:** Internal accounts and JWT.
- **Extensibility:** Keep authentication behind an abstraction to support AD/LDAP integration in the future.

## Local Execution and Deployment
- **Backend Run:** `dotnet run`
- **Frontend Run:** `npm run dev`
- **Deployment:** No IIS, Windows Server, production deployment, or CI/CD pipelines are configured yet.
