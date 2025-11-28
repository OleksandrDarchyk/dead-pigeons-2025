drop schema if exists deadpigeons cascade;
create schema if not exists deadpigeons;

-- ==========================
-- AUTH USERS (for login to the system, not players in the game)
-- ==========================

create table deadpigeons.users
(
    id           text primary key not null,
    email        text             not null,
    passwordhash text             not null,
    salt         text             not null,
    role         text             not null default 'User',
    createdAt    timestamp with time zone,
    deletedAt    timestamp with time zone  -- soft delete for auth users
);

create unique index idx_users_email on deadpigeons.users(email);

-- ==========================
-- PLAYERS - player what admin will register
-- (club members who play the Dead Pigeons game)
-- ==========================

create table deadpigeons.player
(
    id          text primary key not null,
    fullName    text             not null,
    email       text             not null,
    phone       text             not null,
    isActive    boolean          not null default false,
    activatedAt timestamp with time zone,
    createdAt   timestamp with time zone,
    deletedAt   timestamp with time zone
);

create unique index idx_player_email on deadpigeons.player(email);

-- ==========================
-- GAMES (weekly rounds)
-- ==========================

create table deadpigeons.game
(
    id             text primary key not null,
    weekNumber     int              not null,
    year           int              not null,
    winningNumbers int[]            null,
    isActive       boolean          not null default false,
    createdAt      timestamp with time zone,
    closedAt       timestamp with time zone,
    deletedAt      timestamp with time zone
);

create unique index idx_game_week_year on deadpigeons.game(weekNumber, year);

-- ==========================
-- BOARDS (guesses)
-- ==========================

create table deadpigeons.board
(
    id           text primary key not null,
    playerId     text             references deadpigeons.player(id) on delete cascade,
    gameId       text             references deadpigeons.game(id)   on delete cascade,
    numbers      int[]            not null,
    price        int              not null,
    isWinning    boolean          not null default false,
    repeatWeeks  int              not null default 0,
    repeatActive boolean          not null default false,
    createdAt    timestamp with time zone,
    deletedAt    timestamp with time zone
);

-- ==========================
-- TRANSACTIONS (MobilePay)
-- ==========================

create table deadpigeons.transactions
(
    id              text primary key not null,
    playerId        text not null references deadpigeons.player(id) on delete restrict,
    mobilePayNumber text not null,
    amount          int  not null,
    status          text not null default 'Pending',
    createdAt       timestamptz not null default now(),
    approvedAt      timestamptz,
    deletedAt       timestamptz,
    rejectionReason text,

    constraint chk_transactions_amount
        check (amount > 0),

    constraint chk_transactions_status
        check (status in ('Pending', 'Approved', 'Rejected'))
);

create unique index idx_transactions_mp
    on deadpigeons.transactions (mobilePayNumber);


-- ==========================
-- BALANCES
-- ==========================
-- IMPORTANT:
-- We DO NOT store balance in a table.
-- Balance will be calculated in backend:
--   sum(approved transactions.amount) - sum(boards.price) for a player.
-- So we intentionally do NOT create deadpigeons.balance table here.