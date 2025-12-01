// client/src/pages/player/PlayerHistoryPage.tsx
import { useAuth } from "../../atoms/auth";
import { usePlayerHistory } from "../../hooks/usePlayerHistory";
import PlayerGuard from "./PlayerGuard";
import PlayerTabs from "./PlayerTabs";
import type { GameResponseDto } from "../../core/generated-client";

// Helper: read winning numbers from GameResponseDto and sort them
function getWinningNumbers(game: GameResponseDto): number[] {
    // If there are no winning numbers yet, return an empty array
    const numbers = game.winningNumbers ?? [];

    // Return a sorted copy (smallest → biggest)
    return [...numbers].sort((a, b) => a - b);
}

export default function PlayerHistoryPage() {
    const { user, token } = useAuth();
    const role = user?.role;

    // Only real players (role "User") should see this page
    const isPlayer = Boolean(token && user && role === "User");

    // Custom hook returns grouped history (GameResponseDto + BoardResponseDto[])
    const { items, isLoading } = usePlayerHistory(isPlayer);

    return (
        <PlayerGuard>
            <div className="min-h-[calc(100vh-4rem)] bg-slate-50">
                <div className="mx-auto max-w-5xl space-y-6 px-4 py-8">
                    {/* HEADER + TABS */}
                    <header className="pb-1">
                        <h1 className="text-2xl font-bold text-slate-900">
                            Player History
                        </h1>
                        <p className="mt-1 text-sm text-slate-500">
                            Logged in as{" "}
                            <span className="font-medium">
                                {user?.email}
                            </span>
                        </p>

                        <PlayerTabs />
                    </header>

                    {/* Games history content */}
                    {isLoading ? (
                        <p className="text-sm text-slate-500">Loading games...</p>
                    ) : items.length === 0 ? (
                        <p className="text-sm text-slate-500">
                            You have no game history yet.
                        </p>
                    ) : (
                        <div className="space-y-6">
                            {items.map(({ game, boards }) => {
                                const winningNumbers = getWinningNumbers(game);
                                const hasWinningNumbers =
                                    winningNumbers.length > 0;

                                // GameResponseDto uses createdAt (PascalCase)
                                const created =
                                    game.createdAt &&
                                    new Date(game.createdAt).toLocaleDateString();

                                return (
                                    <section
                                        key={game.id}
                                        className="rounded-2xl border border-slate-200 bg-white px-6 py-5 shadow-sm"
                                    >
                                        {/* Round header + status */}
                                        <div className="mb-4 flex items-center justify-between gap-3">
                                            <div>
                                                <p className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-slate-600">
                                                    <span className="inline-flex h-6 w-6 items-center justify-center rounded-full bg-red-50 text-red-500">
                                                        🏆
                                                    </span>
                                                    Week {game.weekNumber},{" "}
                                                    {game.year}
                                                </p>
                                                <p className="mt-1 text-xs text-slate-500">
                                                    Draw date: {created ?? "–"}
                                                </p>
                                            </div>

                                            <span
                                                className={
                                                    "inline-flex items-center rounded-full px-3 py-1 text-xs font-semibold " +
                                                    (hasWinningNumbers
                                                        ? "bg-slate-800 text-white"
                                                        : "bg-green-100 text-green-700")
                                                }
                                            >
                                                {hasWinningNumbers
                                                    ? "Completed"
                                                    : "Active"}
                                            </span>
                                        </div>

                                        {/* Winning numbers */}
                                        <div className="mb-4">
                                            <p className="mb-2 text-sm font-semibold text-slate-900">
                                                Winning Numbers
                                            </p>

                                            {hasWinningNumbers ? (
                                                <div className="flex flex-wrap gap-2">
                                                    {winningNumbers.map((n) => (
                                                        <span
                                                            key={n}
                                                            className="inline-flex h-9 w-9 items-center justify-center rounded-lg bg-red-600 text-sm font-semibold text-white"
                                                        >
                                                            {n}
                                                        </span>
                                                    ))}
                                                </div>
                                            ) : (
                                                <p className="text-xs text-slate-500">
                                                    Winning numbers have not been
                                                    set yet.
                                                </p>
                                            )}
                                        </div>

                                        {/* Boards for this week */}
                                        <div>
                                            <p className="mb-2 text-sm font-semibold text-slate-900">
                                                Your boards for this week
                                            </p>

                                            {boards.length === 0 ? (
                                                <p className="text-xs text-slate-500">
                                                    You did not buy any boards for
                                                    this round.
                                                </p>
                                            ) : (
                                                <div className="space-y-3">
                                                    {boards.map((b) => {
                                                        // How many numbers from this board match the winning ones
                                                        const matchedCount =
                                                            winningNumbers.length >
                                                            0
                                                                ? b.numbers.filter(
                                                                    (n) =>
                                                                        winningNumbers.includes(
                                                                            n
                                                                        )
                                                                ).length
                                                                : 0;

                                                        // What backend says about this board
                                                        const isWinnerFromServer =
                                                            b.isWinning ?? false;

                                                        // Simple backup rule: board is winning
                                                        // if it contains all winning numbers
                                                        const isWinnerByNumbers =
                                                            winningNumbers.length >
                                                            0 &&
                                                            matchedCount ===
                                                            winningNumbers.length;

                                                        // Final winner flag: trust the server first,
                                                        // but keep our own calculation as a backup
                                                        const isWinner =
                                                            isWinnerFromServer ||
                                                            isWinnerByNumbers;

                                                        return (
                                                            <div
                                                                key={b.id}
                                                                className={
                                                                    "rounded-2xl border px-4 py-3 " +
                                                                    (isWinner
                                                                        ? "border-green-400 bg-green-50"
                                                                        : "border-slate-200 bg-slate-50")
                                                                }
                                                            >
                                                                <div className="mb-2 flex flex-wrap items-center justify-between gap-2">
                                                                    {isWinner ? (
                                                                        <span className="inline-flex items-center rounded-full bg-green-500 px-3 py-1 text-xs font-semibold text-white">
                                                                            🎉 Winner!
                                                                        </span>
                                                                    ) : (
                                                                        <span className="inline-flex items-center rounded-full bg-slate-200 px-3 py-1 text-xs font-semibold text-slate-700">
                                                                            Matches:{" "}
                                                                            {
                                                                                matchedCount
                                                                            }
                                                                            /
                                                                            {winningNumbers.length ||
                                                                                3}
                                                                        </span>
                                                                    )}
                                                                </div>

                                                                {/* Board numbers with match highlighting */}
                                                                <div className="flex flex-wrap gap-1">
                                                                    {b.numbers.map(
                                                                        (n) => {
                                                                            const isMatch =
                                                                                winningNumbers.includes(
                                                                                    n
                                                                                );

                                                                            return (
                                                                                <span
                                                                                    key={
                                                                                        n
                                                                                    }
                                                                                    className={
                                                                                        "inline-flex h-7 w-7 items-center justify-center rounded-lg text-xs font-semibold " +
                                                                                        (isMatch
                                                                                            ? "bg-green-500 text-white"
                                                                                            : "bg-slate-300 text-slate-800")
                                                                                    }
                                                                                >
                                                                                    {
                                                                                        n
                                                                                    }
                                                                                </span>
                                                                            );
                                                                        }
                                                                    )}
                                                                </div>
                                                            </div>
                                                        );
                                                    })}
                                                </div>
                                            )}
                                        </div>
                                    </section>
                                );
                            })}
                        </div>
                    )}
                </div>
            </div>
        </PlayerGuard>
    );
}
