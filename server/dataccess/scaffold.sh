#!/bin/bash
set -e  # if something fails, stop the script

# 1) Load CONN_STR from .env (must be in this folder)
set -a
source .env
set +a

# 2) Install dotnet-ef 9.0.4 (if already installed - ignore error)
dotnet tool install --global dotnet-ef --version 9.0.4 || true

# 3) Generate MyDbContext + entities from deadpigeons schema
dotnet ef dbcontext scaffold "$CONN_STR" Npgsql.EntityFrameworkCore.PostgreSQL \
  --context MyDbContext \
  --context-dir . \
  --output-dir Entities \
  --namespace dataccess.Entities \
  --context-namespace dataccess \
  --no-onconfiguring \
  --schema deadpigeons \
  --force
