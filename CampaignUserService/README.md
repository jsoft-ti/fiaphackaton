# CampaignUserService

Microsserviço de autenticação, autorização e gerenciamento de usuários da plataforma de campanhas sociais. Construído em **.NET 9** com **Clean Architecture**, **DDD**, **CQRS (MediatR)** e **PostgreSQL** (Supabase), pronto para execução em contêiner e para integração com outros microsserviços.

## Sumário

- [Arquitetura](#arquitetura)
- [Estrutura de pastas](#estrutura-de-pastas)
- [Requisitos](#requisitos)
- [Como executar](#como-executar)
- [Docker](#docker)
- [Migrations e banco de dados](#migrations-e-banco-de-dados)
- [Configuração do Supabase](#configuração-do-supabase)
- [Variáveis de ambiente](#variáveis-de-ambiente)
- [Autenticação e JWT](#autenticação-e-jwt)
- [Refresh token](#refresh-token)
- [RBAC (perfis e permissões)](#rbac-perfis-e-permissões)
- [Recuperação de senha](#recuperação-de-senha)
- [Endpoints e exemplos de requisição/resposta](#endpoints-e-exemplos-de-requisiçãoresposta)
- [Auditoria](#auditoria)
- [Testes](#testes)
- [Qualidade e segurança](#qualidade-e-segurança)

## Arquitetura

O serviço segue **Clean Architecture** com quatro camadas de código + um Shared Kernel, cada uma só podendo depender das camadas "mais internas":

```
CampaignUserService.Api            -> depende de Application, Infrastructure, SharedKernel
CampaignUserService.Infrastructure -> depende de Application, Domain, SharedKernel
CampaignUserService.Application    -> depende de Domain, SharedKernel
CampaignUserService.Domain         -> depende de SharedKernel
CampaignUserService.SharedKernel   -> não depende de nada (base)
```

- **Domain**: entidades ricas (`User`, `Role`, `UserRole`, `RefreshToken`, `PasswordResetToken`, `AuditLog`), enums, exceções de domínio e as interfaces de repositório/unit of work (Dependency Inversion — o Domain define o contrato, o Infrastructure implementa).
- **Application**: casos de uso organizados por *feature* em CQRS (Commands/Queries + Handlers via MediatR), `FluentValidation` para validação de entrada, `AutoMapper` para projeções e as interfaces de serviços técnicos (`IJwtTokenService`, `IPasswordHasher`, `IEmailSender`, `IAuditService`) que o Infrastructure implementa.
- **Infrastructure**: `DbContext` do EF Core, `Repository`/`UnitOfWork`, segurança (JWT, BCrypt), seed do banco, envio de email (stub pronto para SMTP).
- **Api**: hospeda os *minimal API endpoints*, autenticação JWT Bearer, políticas de autorização (RBAC), Swagger, middlewares (exceções globais, headers de segurança), health checks, rate limiting, versionamento de API.
- **SharedKernel**: `BaseEntity`, `Result`/`Error` (Railway-oriented error handling para regras de negócio) e abstrações comuns (`IDateTimeProvider`, `ICurrentUserService`).

### Padrões aplicados

CQRS com MediatR (pipeline com `ValidationBehavior` e `LoggingBehavior`), Repository + Unit of Work, Options Pattern (`IOptions<T>` para Jwt/AdminSeed/Smtp), Dependency Injection em todas as camadas, ProblemDetails (RFC 7807) para toda resposta de erro, ausência total de segredos hardcoded no código-fonte.

## Estrutura de pastas

```
CampaignUserService/
├── CampaignUserService.sln
├── Directory.Build.props            # Nullable, LangVersion, propriedades comuns
├── Directory.Packages.props         # Central Package Management (versões únicas)
├── Dockerfile
├── docker-compose.yml
├── docker-compose.override.yml
├── .env.example
├── sql/
│   └── schema.sql                   # Bootstrap manual do schema no Supabase
├── src/
│   ├── CampaignUserService.SharedKernel/
│   │   ├── Common/                  # BaseEntity, Result/Result<T>
│   │   ├── Errors/                  # Error, ErrorType
│   │   ├── Exceptions/              # AppException e derivadas
│   │   └── Interfaces/              # IDateTimeProvider, ICurrentUserService
│   ├── CampaignUserService.Domain/
│   │   ├── Entities/                # User, Role, UserRole, RefreshToken, PasswordResetToken, AuditLog
│   │   ├── Enums/                   # UserStatus, RoleName, AuditActionType
│   │   ├── Exceptions/              # DomainException
│   │   └── Repositories/            # Interfaces (IUserRepository, IUnitOfWork, ...)
│   ├── CampaignUserService.Application/
│   │   ├── Common/                  # Behaviors, Interfaces, Models, Mappings
│   │   └── Features/
│   │       ├── Authentication/      # Register, Login, Refresh, Logout, ForgotPassword, ResetPassword
│   │       ├── Users/                # CRUD, self-service, ativação/bloqueio/roles
│   │       └── Roles/                # Consulta e cadastro de roles
│   ├── CampaignUserService.Infrastructure/
│   │   ├── Persistence/              # DbContext, Configurations (EF Fluent API), Repositories, Seed
│   │   ├── Security/                 # JwtTokenService, BCryptPasswordHasher, CurrentUserService
│   │   ├── Services/                 # DateTimeProvider, AuditService, SmtpEmailSender
│   │   └── Options/                  # SmtpSettings
│   └── CampaignUserService.Api/
│       ├── Program.cs
│       ├── appsettings.json / appsettings.Development.json
│       ├── Middleware/               # GlobalExceptionMiddleware, SecurityHeadersMiddleware
│       ├── Extensions/               # Swagger, JWT, CORS, Rate Limiting, Health Checks, Versioning
│       ├── Authorization/            # PolicyNames, policies RBAC
│       ├── Contracts/                # Request DTOs dos endpoints
│       └── Endpoints/                # AuthEndpoints, UsersEndpoints, RolesEndpoints
└── tests/
    ├── CampaignUserService.UnitTests/         # xUnit + FluentAssertions + Moq
    └── CampaignUserService.IntegrationTests/  # WebApplicationFactory + Testcontainers.PostgreSql
```

Cada feature em `Application/Features/*` concentra Command/Query, Validator (FluentValidation) e Handler no mesmo arquivo (ex.: `RegisterCommand.cs`), seguindo o padrão vertical-slice consagrado em templates de Clean Architecture .NET — reduz saltos entre arquivos sem abrir mão de nenhuma camada.

## Requisitos

- [.NET SDK 9.0](https://dotnet.microsoft.com/download/dotnet/9.0)
- PostgreSQL 14+ (local, Docker, ou Supabase)
- Docker + Docker Compose (opcional, para execução em contêiner)
- `dotnet-ef` (opcional, apenas para gerar/aplicar migrations manualmente): `dotnet tool install --global dotnet-ef`

## Como executar

### 1. Localmente com `dotnet run`

```bash
# 1. Restaurar dependências
dotnet restore

# 2. Configurar a connection string (Supabase ou Postgres local) e o segredo JWT
#    (appsettings.Development.json já traz valores de desenvolvimento prontos
#    para uso com um Postgres local via docker-compose)

# 3. Aplicar as migrations (gera o schema no banco configurado)
dotnet ef database update \
  --project src/CampaignUserService.Infrastructure \
  --startup-project src/CampaignUserService.Api

# 4. Executar a API
dotnet run --project src/CampaignUserService.Api
```

A API sobe em `http://localhost:5080` (perfil `http`) com Swagger em `/swagger`.

### 2. Com Docker Compose (API + Postgres local)

```bash
cp .env.example .env
# edite .env e preencha ao menos JWT_SECRET, ADMIN_EMAIL e ADMIN_PASSWORD

docker compose up --build
```

A API sobe em `http://localhost:8080` (Swagger em `/swagger`) e o Postgres local em `localhost:5432`. `docker-compose.override.yml` é aplicado automaticamente e ajusta o ambiente para `Development`.

## Docker

- **Dockerfile**: build multi-stage (`sdk:9.0` para build/publish, `aspnet:9.0` para runtime), roda como usuário não-root, expõe a porta `8080` e define `HEALTHCHECK` usando `/health/live`.
- **docker-compose.yml**: define os serviços `campaign-user-db` (Postgres 16 com volume nomeado e healthcheck via `pg_isready`) e `campaign-user-api` (depende do banco estar saudável), rede dedicada `campaign-user-network`, variáveis de ambiente lidas do `.env`.
- **docker-compose.override.yml**: overrides de desenvolvimento local (ambiente `Development`, portas expostas).
- **.env.example**: modelo de variáveis de ambiente — copie para `.env` e nunca faça commit do `.env` real.

## Migrations e banco de dados

O projeto está pronto para gerar migrations do EF Core assim que o SDK do .NET estiver disponível no ambiente de desenvolvimento:

```bash
dotnet ef migrations add InitialCreate \
  --project src/CampaignUserService.Infrastructure \
  --startup-project src/CampaignUserService.Api

dotnet ef database update \
  --project src/CampaignUserService.Infrastructure \
  --startup-project src/CampaignUserService.Api
```

Um `IDesignTimeDbContextFactory` (`src/CampaignUserService.Infrastructure/Persistence/DesignTimeDbContextFactory.cs`) já está implementado, então os comandos acima funcionam sem precisar subir a API.

Alternativamente — por exemplo, para provisionar rapidamente um banco Supabase sem instalar o SDK do .NET — execute **`sql/schema.sql`** diretamente no SQL Editor do Supabase. O script é o espelho exato do modelo do EF Core (mesmos nomes de tabela/coluna/índice) e semeia as roles `Doador` e `GestorOng`. Nesse caso, defina `Database:AutoMigrateAndSeed=false` (ou `Database__AutoMigrateAndSeed=false`) para a API não tentar aplicar migrations sobre um schema já existente.

Em qualquer um dos dois caminhos, o **usuário GestorOng inicial** é criado automaticamente no primeiro startup da API a partir de `AdminSeed:Email` / `AdminSeed:Password` (ver [Variáveis de ambiente](#variáveis-de-ambiente)) — nenhuma senha de administrador fica hardcoded no código.

### Diagrama do modelo de dados

```
roles (id, name, description, ...)
  └─< user_roles (user_id, role_id) >──┐
                                        │
users (id, first_name, last_name,      │
       email, password_hash, ...)  ────┘
  ├─< refresh_tokens (user_id, token_hash, expires_at_utc, revoked_at_utc, ...)
  ├─< password_reset_tokens (user_id, token_hash, expires_at_utc, used_at_utc)
  └─< audit_logs (user_id, action, description, ip_address, user_agent, occurred_at_utc)
```

## Configuração do Supabase

1. Crie um projeto em [supabase.com](https://supabase.com) e obtenha a *connection string* em **Project Settings → Database → Connection string → URI** (ou a variante "Session pooling" para produção).
2. Monte a connection string no formato Npgsql e configure em `ConnectionStrings:DefaultConnection`:

   ```
   Host=db.<project-ref>.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=<sua-senha>;SSL Mode=Require;Trust Server Certificate=true
   ```

3. Rode as migrations (`dotnet ef database update`) ou execute `sql/schema.sql` no SQL Editor do Supabase.
4. Defina `AdminSeed:Email` / `AdminSeed:Password` e suba a API uma vez — o GestorOng inicial é criado automaticamente.

## Variáveis de ambiente

| Variável | Descrição | Padrão |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | Connection string PostgreSQL (Supabase ou local) | — (obrigatório) |
| `Jwt__Secret` | Segredo usado para assinar os JWTs (mín. 32 caracteres) | — (obrigatório, `ValidateOnStart` falha sem ele) |
| `Jwt__Issuer` | Issuer do token | `CampaignUserService` |
| `Jwt__Audience` | Audience do token | `CampaignUserService.Clients` |
| `Jwt__AccessTokenExpirationMinutes` | Validade do access token | `15` |
| `Jwt__RefreshTokenExpirationDays` | Validade do refresh token | `7` |
| `AdminSeed__FirstName` / `AdminSeed__LastName` | Nome do GestorOng inicial | `Admin` / `Master` |
| `AdminSeed__Email` | Email do GestorOng inicial | — |
| `AdminSeed__Password` | Senha do GestorOng inicial (hasheada com BCrypt no seed) | — |
| `Smtp__Host`, `Smtp__Port`, `Smtp__Username`, `Smtp__Password`, `Smtp__EnableSsl` | Credenciais SMTP para envio real de email | — |
| `Smtp__Enabled` | Quando `false` (padrão), emails são apenas logados, não enviados | `false` |
| `Cors__AllowedOrigins__0`, `__1`, ... | Origens autorizadas para CORS | *(nenhuma — fail closed)* |
| `Database__AutoMigrateAndSeed` | Se `true`, aplica migrations e roda o seed no startup | `true` |

Todos os segredos são lidos via `IConfiguration`/Options Pattern; nenhum valor sensível está hardcoded no código-fonte (`appsettings.json` traz apenas placeholders vazios).

## Autenticação e JWT

- Algoritmo: **HMAC-SHA256**, chave simétrica (`Jwt:Secret`).
- Claims do access token: `sub`/`uid` (id do usuário), `email`, `name`, `role`, `jti` (id único do token, usado para rastreabilidade/revogação futura).
- Validade: **15 minutos** (access token) — configurável via `Jwt:AccessTokenExpirationMinutes`.
- Validação: issuer, audience, assinatura e tempo de vida são todos validados (`ValidateIssuer`, `ValidateAudience`, `ValidateIssuerSigningKey`, `ValidateLifetime`), com `ClockSkew` de 30s.
- **Renovação automática**: o cliente deve chamar `POST /auth/refresh` antes (ou logo após) o access token expirar, usando o refresh token; nenhuma sessão de usuário depende só do access token de curta duração.

## Refresh token

- Token opaco (não é um JWT), gerado com `RandomNumberGenerator` (64 bytes), armazenado no banco **apenas como hash SHA-256** (o valor bruto nunca é persistido).
- Validade: **7 dias** — configurável via `Jwt:RefreshTokenExpirationDays`.
- **Rotação**: a cada uso em `POST /auth/refresh`, o token antigo é revogado e um novo é emitido (`ReplacedByTokenHash` mantém a cadeia de rotação).
- **Blacklist / detecção de reuso**: tokens revogados ficam marcados (`RevokedAtUtc`) e continuam no banco. Se um token já revogado for reapresentado (sinal de possível roubo/replay), **todos os refresh tokens ativos do usuário são revogados automaticamente**, forçando reautenticação em todos os dispositivos.
- `POST /auth/logout` revoga explicitamente o refresh token informado.
- Troca de senha, reset de senha, bloqueio e desativação de conta também revogam todos os refresh tokens ativos do usuário.

## RBAC (perfis e permissões)

Dois perfis fechados, mapeados para claims de role no JWT e aplicados via **Authorization Policies** (nunca via `User.IsInRole(...)` manual):

| Policy | Role exigida | Uso |
|---|---|---|
| `AuthenticatedUser` | qualquer (Doador ou GestorOng) | `/users/me`, `/auth/logout` |
| `DoadorOnly` | `Doador` | reservada para rotas exclusivas de doador (hoje o autocadastro já implica esse perfil) |
| `GestorOngOnly` | `GestorOng` | todas as rotas administrativas de `/users` (exceto `/me`) e `/roles` |

**Doador** pode: criar conta, login, atualizar o próprio perfil, alterar/recuperar a própria senha, consultar os próprios dados, excluir a própria conta.

**GestorOng** pode: tudo que o Doador pode, mais: cadastrar outros usuários (inclusive outros GestorOng), listar/consultar/atualizar/excluir qualquer usuário, ativar/desativar/bloquear usuários, forçar reset de senha de qualquer usuário, alterar a role de qualquer usuário, e gerenciar roles.

## Recuperação de senha

1. `POST /auth/forgot-password { email }` — sempre responde `200 OK`, exista ou não o email (evita enumeração de contas). Se o usuário existir e estiver ativo, um token aleatório (32 bytes, URL-safe) é gerado, apenas seu hash SHA-256 é persistido (`PasswordResetToken`, validade de 1 hora) e o token bruto é "enviado" via `IEmailSender`.
2. `POST /auth/reset-password { token, newPassword, confirmNewPassword }` — valida o token (existe, não usado, não expirado), troca a senha (BCrypt), marca o token como usado e revoga todos os refresh tokens ativos do usuário.
3. `IEmailSender` está implementado como `SmtpEmailSender`: quando `Smtp:Enabled=false` (padrão neste ambiente, sem credenciais reais configuradas), a mensagem é apenas logada via Serilog — todo o fluxo funciona ponta a ponta sem depender de um servidor SMTP real. Basta configurar `Smtp:Host/Port/Username/Password` e setar `Smtp:Enabled=true` para passar a enviar emails de verdade, sem alterar nenhuma linha do Application layer.
4. A senha nunca é enviada por email — apenas o link/token de redefinição.

## Endpoints e exemplos de requisição/resposta

Base path: `/api/v1`. Todas as respostas de erro seguem RFC 7807 (`application/problem+json`).

### `POST /api/v1/auth/register`

```json
// Request
{
  "firstName": "Maria",
  "lastName": "Silva",
  "email": "maria.silva@example.com",
  "password": "Str0ng!Pass",
  "confirmPassword": "Str0ng!Pass",
  "phoneNumber": "+55 11 91234-5678",
  "cpf": "12345678901",
  "birthDate": "1995-04-12"
}
```

```json
// Response 200 OK
{
  "accessToken": "eyJhbGciOi...",
  "refreshToken": "5f3G...==",
  "accessTokenExpiresAtUtc": "2026-07-22T15:15:00Z",
  "tokenType": "Bearer",
  "userId": "b6f1...",
  "email": "maria.silva@example.com",
  "fullName": "Maria Silva",
  "role": "Doador"
}
```

```json
// Response 409 Conflict (email já cadastrado)
{
  "type": "https://httpstatuses.io/409",
  "title": "Conflito de dados.",
  "status": 409,
  "detail": "Já existe uma conta cadastrada com este email.",
  "code": "email_already_used"
}
```

### `POST /api/v1/auth/login`

```json
// Request
{ "email": "maria.silva@example.com", "password": "Str0ng!Pass" }
```

Resposta: igual ao `register` (mesmo `AuthResultDto`). Erros possíveis: `401` (`invalid_credentials`), `403` (`user_blocked` / `user_inactive`).

### `POST /api/v1/auth/refresh`

```json
// Request
{ "refreshToken": "5f3G...==" }
```

Resposta: novo `AuthResultDto` (access + refresh token rotacionados). `401` se o token for inválido, expirado ou já revogado.

### `POST /api/v1/auth/logout` *(requer Authorization: Bearer)*

```json
// Request
{ "refreshToken": "5f3G...==" }
```

`200 OK` (idempotente).

### `POST /api/v1/auth/forgot-password`

```json
{ "email": "maria.silva@example.com" }
```

`200 OK` sempre.

### `POST /api/v1/auth/reset-password`

```json
{ "token": "aB3f...", "newPassword": "N0v@Senha!", "confirmNewPassword": "N0v@Senha!" }
```

### `GET /api/v1/users/me` *(Authorization: Bearer)*

```json
// Response 200 OK
{
  "id": "b6f1...",
  "firstName": "Maria",
  "lastName": "Silva",
  "email": "maria.silva@example.com",
  "phoneNumber": "+55 11 91234-5678",
  "cpf": "12345678901",
  "photoUrl": null,
  "birthDate": "1995-04-12",
  "status": "Active",
  "emailConfirmed": false,
  "role": "Doador",
  "createdAtUtc": "2026-07-22T14:58:00Z",
  "updatedAtUtc": null,
  "lastLoginAtUtc": "2026-07-22T15:00:00Z"
}
```

### Demais endpoints

| Método | Rota | Policy | Descrição |
|---|---|---|---|
| `PUT` | `/users/me` | AuthenticatedUser | Atualiza o próprio perfil |
| `PUT` | `/users/me/password` | AuthenticatedUser | Altera a própria senha |
| `DELETE` | `/users/me` | AuthenticatedUser | Exclui (soft delete) a própria conta |
| `GET` | `/users` | GestorOngOnly | Lista usuários (`?search=&role=&status=&page=&pageSize=`) |
| `GET` | `/users/{id}` | GestorOngOnly | Consulta usuário por id |
| `POST` | `/users` | GestorOngOnly | Cria usuário (Doador ou GestorOng) |
| `PUT` | `/users/{id}` | GestorOngOnly | Atualiza perfil de qualquer usuário |
| `DELETE` | `/users/{id}` | GestorOngOnly | Exclui (soft delete) qualquer usuário |
| `PATCH` | `/users/{id}/activate` | GestorOngOnly | Ativa usuário |
| `PATCH` | `/users/{id}/deactivate` | GestorOngOnly | Desativa usuário |
| `PATCH` | `/users/{id}/block` | GestorOngOnly | Bloqueia usuário |
| `PATCH` | `/users/{id}/roles` | GestorOngOnly | Altera a role do usuário |
| `POST` | `/users/{id}/reset-password` | GestorOngOnly | Força envio de link de reset de senha |
| `GET` | `/roles` | GestorOngOnly | Lista as roles do sistema |
| `POST` | `/roles` | GestorOngOnly | Cadastra/atualiza descrição de uma role |
| `GET` | `/health`, `/health/live`, `/health/ready` | — | Health checks |

## Auditoria

Toda operação sensível grava um `AuditLog` (usuário, ação, descrição, IP, User-Agent, timestamp UTC), persistido de forma independente da transação principal (nunca quebra o fluxo do usuário mesmo se a auditoria falhar): registro/cadastro, login (sucesso e falha), logout, alteração/reset de senha, criação/atualização/exclusão de usuário, ativação/desativação/bloqueio, troca de role, emissão/revogação de refresh token.

## Testes

```bash
# Unitários (xUnit + FluentAssertions + Moq) - não dependem de infraestrutura externa
dotnet test tests/CampaignUserService.UnitTests

# Integração (WebApplicationFactory + Testcontainers.PostgreSql) - requer Docker em execução
dotnet test tests/CampaignUserService.IntegrationTests

# Todos, com relatório de cobertura
dotnet test --collect:"XPlat Code Coverage"
```

Os testes unitários cobrem handlers de Authentication/Users (register, login, change password), validadores FluentValidation, `BCryptPasswordHasher` e `JwtTokenService`. Os testes de integração sobem a API completa contra um PostgreSQL real via Testcontainers e cobrem os fluxos de registro/login/refresh (incluindo detecção de reuso de refresh token) e RBAC em `/users`.

## Qualidade e segurança

- **SOLID / Clean Code / DRY / KISS**: camadas com responsabilidade única, injeção de dependência em todos os pontos de extensão, Options Pattern em vez de acesso direto a `IConfiguration` nos handlers.
- **BCrypt** (work factor 12) para hash de senha; senha nunca é logada ou retornada em nenhuma resposta.
- **Rate limiting**: 10 req/min por IP em `/auth/*`, 100 req/min por IP nas demais rotas (`Microsoft.AspNetCore.RateLimiting`).
- **CORS**: fail-closed por padrão — só origens explicitamente listadas em `Cors:AllowedOrigins` são aceitas.
- **HTTPS/HSTS** habilitados fora de `Development`; headers de segurança (`X-Content-Type-Options`, `X-Frame-Options`, `Content-Security-Policy`, `Referrer-Policy`, `Permissions-Policy`) aplicados a toda resposta via `SecurityHeadersMiddleware`.
- **Proteção a SQL Injection**: 100% do acesso a dados via EF Core parametrizado (LINQ), nenhuma concatenação de SQL.
- **Validação de input**: FluentValidation em todo Command/Query, executado no pipeline do MediatR antes de qualquer handler rodar (fail fast).
- **ProblemDetails (RFC 7807)** em toda resposta de erro, com `GlobalExceptionMiddleware` cobrindo qualquer exceção não tratada — nenhuma stack trace é exposta fora de `Development`.
- **Nullable Reference Types** habilitado em todos os projetos (`Directory.Build.props`).
- **Central Package Management** (`Directory.Packages.props`) — uma única fonte de verdade para versões de pacotes.
