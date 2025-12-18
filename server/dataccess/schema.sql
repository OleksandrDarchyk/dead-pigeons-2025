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
    role         text             not null default 'User',
    createdAt    timestamp with time zone,
    deletedAt    timestamp with time zone  -- soft delete for auth users
);

create unique index idx_users_email on deadpigeons.users(email);

-- ==========================
-- PLAYERS - players registered by admin
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

-- Unique game per week (ISO week + year)
create unique index idx_game_week_year
    on deadpigeons.game(weekNumber, year);

-- AT ANY GIVEN TIME THERE CAN BE AT MOST ONE ACTIVE GAME
-- (only one row with isActive = true)
create unique index idx_game_single_active
    on deadpigeons.game(isActive)
    where isActive = true;

-- ==========================
-- BOARDS (guesses)
-- ==========================

create table deadpigeons.board
(
    id           text primary key not null,

    -- IMPORTANT: on delete RESTRICT, because we use soft delete
    -- and do not want history to be removed by cascade delete
    playerId     text             references deadpigeons.player(id) on delete restrict,
    gameId       text             references deadpigeons.game(id)   on delete restrict,

    numbers      int[]            not null,

    -- IMPORTANT:
    -- price is always the price for ONE game (one week),
    -- not the total prepaid amount for multiple weeks.
    price        int              not null,

    isWinning    boolean          not null default false,

    -- repeatWeeks now means: how many FUTURE games this board
    -- should still auto-repeat for (remaining repeats).
    repeatWeeks  int              not null default 0,

    -- repeatActive indicates whether auto-repeat is currently enabled.
    repeatActive boolean          not null default false,

    createdAt    timestamp with time zone,
    deletedAt    timestamp with time zone,

    constraint chk_board_price_positive
        check (price > 0)
);

-- ==========================
-- TRANSACTIONS (MobilePay)
-- ==========================

create table deadpigeons.transactions
(
    id              text primary key not null,

    -- Correct approach: on delete RESTRICT
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

-- Balance is calculated in the backend as:
--   sum(approved transactions.amount) - sum(boards.price) per player.
-- That is why  intentionally do NOT create a deadpigeons.balance table here.
