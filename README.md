# Todulip

Todulip is a todo application with users via Clerk, categorized lists, sortable list items, due dates, completion tracking, and archived/completed list workflows.

## Architecture

The repository is split into two applications:

- `app/`: React, TypeScript, and Vite frontend. Clerk handles authentication, API access is centralized under `src/api`, and the UI is organized around shared components, the sidebar list browser, and the active list/task workspace.
- `service/`: ASP.NET Core Web API targeting .NET 10. Entity Framework Core persists data to SQLite, controllers expose list/category/item endpoints, services hold application logic, validators enforce entity rules, and query extensions keep filtering/sorting behavior composable.

The API authenticates Clerk JWTs, creates or resolves the local user record per request, applies user ownership filters in the data layer, and runs EF Core migrations on startup outside the test environment. Swagger is enabled in development.

## Scalability
- The backend application cannot be scaled horizontally due to the SQLite backend.
- Moving the backend persistence to PostgreSQL (or another database) will allow horizontal scaling of the service - multiple services can be spun up with a single database to process more requests (and the database will scale vertically).
- Correlation IDs are already included in error responses; a production deployment would send structured logs and metrics to centralized monitoring.

## Performance Improvements
- As data grows, list/item queries should be reviewed with real query plans and supported with indexes aligned to ownership, parent list, due date, completion, and sort fields.
- The frontend is chatty and does not implement any caching for items it has locally. Since the lists are currently owned and manipulated by a single user, it makes sense to locally cache API results and mutate the cache when update/delete operations are called. This will also enable the list of lists to update more dynamically.


## Decision Points

- Authentication and user provisioning:
  - For production-worthy auth it made sense to use a third-party. ([Clerk](https://clerk.com/)) provides an attractive offering with libraries and packages that can be easily integrated along with a prebuilt UI. Since we are using Clerk, a new user database record is created when the API is accessed with a new Clerk token. This can be fleshed out further to provide a full user profile and settings, and works well for now given the transient state of SQLite.
- Data ownership and access control:
  - Records are owned by their creators with no option for sharing. Ownership is enforced via a WhereCurrentUserHasAccess check in the database for user-created entities. Check could be easily expanded if a sharing mechanism was created.
  - Access is controlled via the auth Id on the Clerk token, which maps to a user Id. The middleware retrieves the Id from the database and provides that as user context.
- List/item search, sorting, and pagination:
  - Searching is implemented via SearchCriteria objects, which contain sorting and pagination fields.
  - Front-end sort fields are mapped to keys on the back end (organized under the Entity -> SortMappings).
  - Mappings are processed via SortEntity, which uses the key provided by the front-end to order by that field. Multiple sort criteria are accepted, but multi-sort on the front-end has not yet been implemented.
- Date handling and overdue status:
  - Items can have due dates. These due dates allow calculation of pending or overdue status. Overdue is anything with a due date before today (that is not completed). List icons are derived from the soonest due date on the item. Statuses for items are complete, pending, or overdue. Due soon is anything due within the next two days.
- Validation and error handling:
  - Validation is sparse on the front-end. Validation is provided via attributes on the back-end request models. Validation guards against length of text strings being passed back to the server.
  - Other than length, validation is limited (e.g., users can set due dates in the past)
  - Errors are handled by translating the back-end errors via a generic popup. Errors are sanitized via the `ExceptionHandlingMiddleware`.

- Known limitations or future improvements:
  - Front-end validation should be improved - there is no front-end validation, so the user needs to have a request fail before knowing there is a problem.
  - The initial implementation of categories was meant to allow custom categories to be created. This was not implemented, but is planned as a future feature. Static categories could have been done via an enum vs a database table.
  - The authentication flow could be improved. There is no user profile (or even user name) stored within the app, we just create an identifier off of the Clerk Id. Clerk has webhooks and other features we could use to build and improve the user profile/settings. For now, the back-end is only using the Id.
  - The current swagger implementation requires you to log in to the front-end of the app and retrieve the token from a request. This is not ideal - flow here can be improved.
  - Manual testing of the API currently requires a real Clerk token. This can be improved by leveraging a local dev auth profile.
  - The frontend does not use routing, so individual lists cannot be bookmarked or linked. Given the single-workspace layout and the ability to complete or archive lists, the current navigation model is acceptable.
  - The backend uses ASP.NET Core request limiting (`AddRateLimiter`) to limit requests to 100 per IP per minute. Once hosted in a production environment behind a gateway/load balancer, this rate limit should be applied at the gateway level.
  - The frontend sidebar does not update until the user performs a refresh. The sidebar will become stale as the user changes the list items. When a local cache is implemented, this may be improved. As the frontend is already chatty, I did not want to initiate a new search call each time an item was updated.

## Prerequisites

- Node.js `^20.19.0` or `>=22.12.0`
- npm
- .NET 10 SDK
- A Clerk application with a publishable key and issuer/authority URL


## Configuration

Frontend configuration lives in `app/.env`.

```env
VITE_CLERK_PUBLISHABLE_KEY=pk_test_your_clerk_publishable_key
VITE_API_BASE_URL=http://localhost:5135/api
```

Backend configuration lives in `service/Api/appsettings.json` and `service/Api/appsettings.Development.json`.

Key settings:

- `Clerk:Authority`: Clerk issuer URL used to validate JWTs.
- `Cors:AllowedOrigins`: frontend origins allowed to call the API.
- `ConnectionStrings:DefaultConnection`: SQLite database path. The default is `Data Source=data/todo.db`.

**Note:** the Clerk publishable key on the front-end and the Clerk:Authority must belong to the same Clerk application.

## Run Locally

### Docker Compose

Docker Compose can run both the API and frontend without installing Node.js or the .NET SDK locally.

```bash
cp .env.example .env
docker compose up --build
```

Before starting, update `.env` with your Clerk values. `VITE_CLERK_PUBLISHABLE_KEY` and `CLERK_AUTHORITY` must belong to the same Clerk application.

The compose stack exposes:

- Frontend: `http://localhost:5173`
- API: `http://localhost:5135`
- Swagger: `http://localhost:5135/swagger`
- Health check: `http://localhost:5135/health`

The API stores SQLite data in the `todo-api-data` Docker volume. If either host port is already in use, change `WEB_PORT` or `API_PORT` in `.env`; if you change `API_PORT`, also update `VITE_API_BASE_URL`. Because Vite reads `VITE_*` values at build time, rebuild the frontend image after changing `.env`.

### Manual Development

Start the API:

```bash
cd service/Api
dotnet restore
dotnet run --launch-profile http
```

Migrations run automatically.
The API runs at `http://localhost:5135`.

- Swagger: `http://localhost:5135/swagger`
- Health check: `http://localhost:5135/health`

Start the frontend in a second terminal:

```bash
cd app
npm install
cp .env.example .env
```

After creating `app/.env`, update `VITE_CLERK_PUBLISHABLE_KEY` and set `VITE_API_BASE_URL` to `http://localhost:5135/api`.

Then start Vite:

```bash
npm run dev
```

The frontend runs at `http://localhost:5173`.

## Troubleshooting

- `401 Unauthorized`: Confirm the frontend `VITE_CLERK_PUBLISHABLE_KEY` and backend `Clerk:Authority` belong to the same Clerk application. If using Swagger, copy a fresh token from a signed-in frontend request and remove the leading `Bearer ` text before pasting it into Swagger.
- CORS errors: Confirm the Vite origin shown in the browser is listed under `Cors:AllowedOrigins` in the backend configuration. The default development origin is `http://localhost:5173`.
- Frontend cannot reach the API: Confirm the API is running with `dotnet run --launch-profile http` and that `VITE_API_BASE_URL` is set to `http://localhost:5135/api`.
- Empty or stale local data: The default SQLite database is created at `service/Api/data/todo.db`. Migrations run automatically when the API starts outside the test environment.
- Empty or stale Docker data: Compose stores SQLite data in the `todo-api-data` volume. Run `docker compose down -v` to reset it.
- Swagger requests fail after previously working: Clerk session tokens are short-lived. Copy a fresh token from the frontend and authorize Swagger again.
- Vite install or startup fails: Confirm the local Node version satisfies `^20.19.0` or `>=22.12.0`.

## Use Swagger

Swagger is available in development at `http://localhost:5135/swagger`. The API uses Clerk JWT bearer authentication, so Swagger needs a valid Clerk session token before secured endpoints will work.

1. Start the API and frontend.
2. Sign in through the frontend at `http://localhost:5173`.
3. Open browser devtools and trigger any API request from the app.
4. In the request headers, copy the `Authorization` header value.
5. Open Swagger, select `Authorize`, and paste only the JWT value - remove the leading `Bearer ` text (including the space).
6. Run API requests from Swagger.

If Swagger returns `401 Unauthorized`, copy a fresh token from the frontend. Clerk session tokens are short-lived.

## Use the App

1. Open `http://localhost:5173`.
2. Sign in with Clerk.
3. Create lists from the sidebar and assign categories.
4. Select a list to add, rename, complete, delete, sort, and page through list items.
5. Use due dates to distinguish pending and overdue items.
6. Archive or complete lists when they are no longer active.

## Tests and Checks

Backend:

```bash
cd service
dotnet test
```

Frontend:

```bash
cd app
npm run lint
npm test -- --run
npm run build
```

## Project Structure

```text
todo/
  app/
    Dockerfile
    nginx.conf
    src/
      api/              API clients and shared fetch/error handling
      components/       React components grouped by feature area
      utils/            Shared frontend utilities
      test/             Frontend test setup
  service/
    Dockerfile
    Api/
      Controllers/      HTTP API endpoints
      Services/         Application logic
      Infrastructure/   EF Core context, entities, extensions, migrations
      Models/           Request and response contracts
      Middleware/       Cross-cutting request handling
      Validators/       Entity validation
    Api.Tests/          Unit and integration tests
```
