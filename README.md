# Gestor de Vendas — .NET 8 + Blazor Server + PostgreSQL

Sistema de gestão multi-tenant para padarias e confeitarias.

---

## Perfis de acesso

| Perfil | Acesso |
|--------|--------|
| **Admin** | Cadastro de empresas e gerentes. Visão global do sistema. |
| **Gerente** | PDV (com desconto), produtos, estoque, vendas, relatórios, operadores, configurações. |
| **Operador** | Apenas PDV (sem desconto) e histórico das próprias vendas. |

**Usuário padrão:** `admin` / `admin`

---

## Tecnologias

- .NET 8 + Blazor Server
- PostgreSQL + Entity Framework Core 8
- Bootstrap 5 + Bootstrap Icons
- JWT (stateless, 10h de validade)
- Docker + Docker Compose

---

## Rodar localmente

### Opção A — Docker Compose (recomendado)

```bash
docker compose up --build
```

Acesse: http://localhost:8080

### Opção B — .NET direto

1. Instale o [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Instale o [PostgreSQL](https://www.postgresql.org/download/)
3. Ajuste a string de conexão em `GestorVendas.Web/appsettings.Development.json`
4. Execute:

```bash
cd GestorVendas.Web
dotnet run
```

O banco é criado automaticamente ao iniciar (`EnsureCreated` + seed).

---

## Deploy no Railway

### 1. Criar projeto

No [Railway](https://railway.app), crie um novo projeto e adicione:
- Um serviço **PostgreSQL** (plug-in nativo)
- Um serviço da **aplicação** apontando para este repositório

### 2. Variáveis de ambiente

No serviço da aplicação, configure:

```
ConnectionStrings__Default=Host=${{PGHOST}};Port=${{PGPORT}};Database=${{PGDATABASE}};Username=${{PGUSER}};Password=${{PGPASSWORD}}
Jwt__Key=SuaChaveSuperSecretaAquiMinimo32Caracteres!
Jwt__Issuer=GestorVendas
Jwt__Audience=GestorVendas
ASPNETCORE_ENVIRONMENT=Production
```

> O Railway injeta automaticamente as variáveis `PGHOST`, `PGPORT`, `PGDATABASE`, `PGUSER` e `PGPASSWORD` do banco PostgreSQL vinculado.

### 3. Dockerfile

O Railway detecta o `Dockerfile` automaticamente na raiz do repositório.

### 4. Deploy

Faça push para o repositório vinculado. O Railway faz build e deploy automaticamente.

---

## Estrutura do projeto

```
GestorVendas/
├── GestorVendas.Domain/           # Entidades, enums, interfaces
│   ├── Entities/Entities.cs
│   ├── Enums/Enums.cs
│   └── Interfaces/Interfaces.cs
│
├── GestorVendas.Application/      # Regras de negócio
│   ├── DTOs/DTOs.cs
│   ├── Interfaces/IServices.cs
│   └── Services/
│       ├── AuthService.cs
│       ├── EmpresaUsuarioService.cs
│       └── Services.cs            # Produto, Venda, Dashboard, Relatório, Config
│
├── GestorVendas.Infra/            # EF Core, repositórios, banco
│   ├── Data/AppDbContext.cs       # DbContext + configurações + seed
│   ├── Repositories/Repositories.cs
│   └── InfraExtensions.cs
│
├── GestorVendas.Web/              # Blazor Server (frontend + API juntos)
│   ├── Pages/
│   │   ├── Login.razor
│   │   ├── Admin/
│   │   │   ├── AdminDashboard.razor
│   │   │   ├── Empresas.razor
│   │   │   └── AdminUsuarios.razor
│   │   ├── Gerente/
│   │   │   ├── GerenteDashboard.razor
│   │   │   ├── Produtos.razor
│   │   │   ├── Estoque.razor
│   │   │   ├── Vendas.razor
│   │   │   ├── Relatorios.razor
│   │   │   ├── Usuarios.razor
│   │   │   └── Configuracoes.razor
│   │   └── PDVPage.razor          # Compartilhado: /gerente/pdv e /operador/pdv
│   ├── Shared/
│   │   ├── MainLayout.razor       # Sidebar com nome da empresa
│   │   ├── EmptyLayout.razor
│   │   └── RedirectToLogin.razor
│   ├── Services/SessaoService.cs  # Estado da sessão JWT
│   ├── Program.cs
│   └── appsettings.json
│
├── Dockerfile
├── docker-compose.yml
└── .dockerignore
```

---

## Próximos módulos sugeridos

- [ ] **NF-e / NFC-e** — estrutura de entidade já preparada no banco
- [ ] **Impressão de cupom** — ESC/POS para impressora térmica
- [ ] **Relatório em PDF** — exportação do relatório de vendas
- [ ] **Backup automático** — dump do PostgreSQL agendado
- [ ] **Multi-caixa** — controle de abertura/fechamento de caixa por turno
