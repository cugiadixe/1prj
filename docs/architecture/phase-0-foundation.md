# Phase 0: Foundation

**Goal**: Establish the project skeleton and technical foundation.

## Components

- **Backend**: ASP.NET Core Web API structured by Vertical Slices (Api, Application, Domain, Infrastructure, Worker, DbMigrator). Target Framework: .NET 10.
- **Frontend**: React, Vite, TypeScript, Ant Design, TanStack Query, React Hook Form, Zod.
- **Database**: Structure for raw SQL migrations, rollbacks, and a custom `DbMigrator` console app.
- **Testing**: xUnit, NSubstitute, WebApplicationFactory for backend. Vitest, React Testing Library for frontend.
- **Standardization**: API at http://localhost:5057, Frontend at http://localhost:5173. Cross-Origin Resource Sharing (CORS) configured. Correlation IDs implemented. Real SQL health checks and application health tests verified.

This phase deliberately contains no business logic. It provides the setup required to begin Phase 1.
