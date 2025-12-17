# Dead Pigeons Project

Dead Pigeons is a web-based system that digitizes Jerne IF’s weekly “lottery-style” number game. 
It provides an admin dashboard to manage players, weekly games, winning numbers, transactions, 
and boards, and a player dashboard to deposit money, buy boards, and track history and results.

---

##

| Service | URL |
|--------|-----|
| Frontend (React) | https://deadpigeons-client-one.fly.dev/ |
| Backend (.NET API) | https://dead-pigeons-server-one.fly.dev/ |

---

## Login

**Admin**
- Email: `a@dp.dk`
- Password: `Password123`

**Player**
- Option A : The **Admin must create the player and set the player as Active**.  
  After that, the player can register/login in the client.

- Option B (use already created player):
    - Email: `p@dp.dk`
    - Password: `Password123`

    

---

## Features

### Admin Dashboard
- Manage **Players** (create/update, activate/inactivate, soft delete)
- Manage **Payments / Transactions** (create, approve/reject, view history)
- Set **Winning Numbers** for the weekly round (3 numbers)
- View **Boards & Stats** (boards per game, basic statistics)

### Player Dashboard
- View **Balance** (calculated from approved payments and board purchases)
- **Buy boards** for the active weekly game
- View **My boards** and **history**
- View **My payments / transactions**

---

## Core Business Rules (Dead Pigeons)

- Weekly games (rounds)
- A board contains **5–8 numbers** in range **1–16**
- Board price depends on the count of numbers: **20 / 40 / 80 / 160 DKK**
- Admin manually enters **3 winning numbers**
- **Balance is NOT stored in DB**  
  Balance = `SUM(Approved transactions) - SUM(Board purchases)`
- **Soft delete** is used (entities are not physically removed)
- Repeating boards for **X weeks** supported (when enabled)

---

## Technology Stack

| Area | Tech |
|------|------|
| Frontend | React + TypeScript + Vite |
| UI | TailwindCSS + DaisyUI |
| State | Jotai |
| Backend | .NET 9 Web API |
| Database | PostgreSQL |
| Auth | JWT (role-based: Admin / Player) |
| API Docs | OpenAPI (NSwag / Swagger / Scalar depending on environment) |
| Testing | xUnit + Xunit.DependencyInjection + Testcontainers (PostgreSQL) |
| CI | GitHub Actions (frontend lint/build, backend build/test) |
| Deployment | Fly.io (separate apps for client and server) |
| Tooling | Docker / Docker Compose |

---

## Installation and Setup (Local)

## Quick Start (Local)

| Step | What to do | Command (run from repo root unless noted) |
|------|------------|-------------------------------------------|
| 1 | Start database (PostgreSQL) | `docker compose up -d` |
| 2 | Start backend (.NET API) | `dotnet run --project server/api` |
| 3 | Start frontend (React) | `cd client`<br/>`npm ci`<br/>`npm run dev` |
| 4 | Open in browser | Frontend: http://localhost:5173 |

> Tip: To stop the database containers: `docker compose down`


###  Clone repository
```bash
git clone https://github.com/OleksandrDarchyk/dead-pigeons-2025.git

