// client/src/pages/player/PlayerDashboardPage.tsx
import { Link } from "react-router-dom";
import { useAuth } from "../../atoms/auth";
import PlayerPaymentsSection from "./PlayerPaymentsSection";
import { usePlayerBoards } from "../../hooks/usePlayerBoards";
import { usePlayerBalance } from "../../hooks/usePlayerBalance";
import PlayerGuard from "./PlayerGuard";
import type { BoardResponseDto } from "@core/generated-client";
import toast from "react-hot-toast";
import PlayerTabs from "./PlayerTabs";

export default function PlayerDashboardPage() {
    const { user, token } = useAuth();

    const role = user?.role;
    const isPlayer = Boolean(token && user && role === "User");

    const {
        balance,
        isLoading: isBalanceLoading,
    } = usePlayerBalance(isPlayer);

    const {
        boards,
        activeGame,
        isLoading: isBoardsLoading,
    } = usePlayerBoards(isPlayer);

    // Server already filters out soft-deleted boards, so we can use boards directly
    const visibleBoards = boards;

    const handleStopRepeating = async (board: BoardResponseDto) => {
        // Placeholder: repeating logic will be implemented later
        console.log("Stop repeating board", board.id);
        toast("Stopping repeating boards will be implemented on the server later.", {
            icon: "ℹ️",
        });
    };

    return (
        <PlayerGuard>
            <div className="min-h-[calc(100vh-4rem)] bg-slate-50">
                <div className="mx-auto max-w-5xl space-y-6 px-4 py-8">
                    {/* HEADER WITH TABS */}
                    <header className="pb-1">
                        <h1 className="text-2xl font-bold text-slate-900">
                            Player Dashboard
                        </h1>
                        <p className="mt-1 text-sm text-slate-500">
                            Logged in as{" "}
                            <span className="font-medium">{user?.email}</span>
                        </p>

                        <PlayerTabs />
                    </header>

                    {/* CURRENT BALANCE */}
                    <section className="flex items-center justify-between rounded-2xl border border-red-100 bg-gradient-to-r from-red-50 to-rose-50 px-6 py-6 shadow-sm">
                        <div>
                            <p className="text-xs font-semibold uppercase tracking-wide text-red-500">
                                Current Balance
                            </p>

                            {isBalanceLoading && balance === null ? (
                                <p className="mt-2 text-sm text-slate-500">
                                    Loading balance...
                                </p>
                            ) : (
                                <p className="mt-2 text-3xl font-extrabold text-red-600">
                                    {(balance ?? 0).toFixed(2)} DKK
                                </p>
                            )}

                            <p className="mt-1 text-xs text-slate-500">
                                Available for board purchases.
                            </p>
                        </div>

                        <Link
                            to="/balance"
                            className="rounded-full bg-red-600 px-5 py-2 text-xs font-semibold text-white hover:bg-red-700"
                        >
                            Add Funds
                        </Link>
                    </section>

                    {/* ACTIVE ROUND */}
                    <section className="flex items-center justify-between rounded-2xl border border-slate-200 bg-white px-6 py-5 shadow-sm">
                        <div>
                            <p className="text-xs font-semibold uppercase tracking-wide text-slate-600">
                                Active Round
                            </p>

                            {activeGame ? (
                                <>
                                    <p className="mt-1 text-xs text-slate-500">
                                        Current week&apos;s lottery round
                                    </p>
                                    <p className="mt-2 text-sm font-semibold text-slate-900">
                                        Week {activeGame.weekNumber},{" "}
                                        {activeGame.year}
                                    </p>

                                    <div className="mt-2 inline-flex items-center rounded-full bg-green-100 px-3 py-1 text-xs font-semibold text-green-700">
                                        Active
                                    </div>
                                </>
                            ) : (
                                <p className="mt-2 text-sm text-slate-500">
                                    No active round at the moment.
                                </p>
                            )}
                        </div>

                        {activeGame ? (
                            <Link
                                to="/buy-board"
                                className="rounded-full bg-red-100 px-5 py-2 text-xs font-semibold text-red-700 hover:bg-red-200"
                            >
                                Buy Board
                            </Link>
                        ) : (
                            <button
                                type="button"
                                disabled
                                className="rounded-full bg-red-100 px-5 py-2 text-xs font-semibold text-red-700 opacity-60"
                            >
                                Buy Board
                            </button>
                        )}
                    </section>

                    {/* MY PAYMENTS */}
                    <PlayerPaymentsSection />

                    {/* MY BOARDS */}
                    <section className="rounded-2xl border border-slate-200 bg-white px-6 py-5 shadow-sm">
                        <h2 className="mb-1 text-lg font-semibold text-slate-900">
                            My Boards
                        </h2>
                        <p className="mb-4 text-xs text-slate-500">
                            Boards you have bought for current and previous
                            games.
                        </p>

                        {isBoardsLoading ? (
                            <p className="text-sm text-slate-500">
                                Loading boards...
                            </p>
                        ) : visibleBoards.length === 0 ? (
                            <p className="text-sm text-slate-500">
                                You have no boards yet. Once you buy a board, it
                                will appear here.
                            </p>
                        ) : (
                            <div className="space-y-4">
                                {visibleBoards.map((b) => {
                                    const created =
                                        b.createdAt &&
                                        new Date(
                                            b.createdAt
                                        ).toLocaleDateString();

                                    const isRepeating = b.repeatActive;
                                    const numbers = [...b.numbers].sort(
                                        (a, c) => a - c
                                    );

                                    return (
                                        <div
                                            key={b.id}
                                            className={
                                                "flex flex-col gap-3 rounded-2xl border px-4 py-4 md:flex-row md:items-center md:justify-between " +
                                                (isRepeating
                                                    ? "border-red-300 bg-red-50/60"
                                                    : "border-slate-200 bg-slate-50")
                                            }
                                        >
                                            <div className="space-y-1">
                                                <div className="flex flex-wrap items-center gap-2">
                                                    {isRepeating ? (
                                                        <span className="inline-flex items-center rounded-full bg-green-100 px-3 py-1 text-xs font-semibold text-green-700">
                                                            Repeating
                                                        </span>
                                                    ) : (
                                                        <span className="inline-flex items-center rounded-full bg-slate-200 px-3 py-1 text-xs font-semibold text-slate-700">
                                                            Single Round
                                                        </span>
                                                    )}

                                                    <span className="text-xs text-slate-500">
                                                        Purchased:{" "}
                                                        {created ?? "–"}
                                                    </span>
                                                </div>

                                                <p className="text-xs text-slate-500">
                                                    Price:{" "}
                                                    <span className="font-semibold text-slate-900">
                                                        {b.price.toFixed(2)} DKK
                                                    </span>
                                                </p>

                                                {b.isWinning && (
                                                    <span className="inline-flex items-center rounded-full bg-amber-100 px-3 py-1 text-xs font-semibold text-amber-800">
                                                        Winning board
                                                    </span>
                                                )}
                                            </div>

                                            <div className="flex flex-wrap items-center gap-3">
                                                <div className="flex flex-wrap gap-1">
                                                    {numbers.map((n) => (
                                                        <span
                                                            key={n}
                                                            className="inline-flex h-7 w-7 items-center justify-center rounded-lg bg-red-600 text-xs font-semibold text-white"
                                                        >
                                                            {n}
                                                        </span>
                                                    ))}
                                                </div>

                                                {isRepeating && (
                                                    <button
                                                        type="button"
                                                        onClick={() =>
                                                            void handleStopRepeating(
                                                                b
                                                            )
                                                        }
                                                        className="rounded-full border border-red-300 px-3 py-1 text-xs font-semibold text-red-700 hover:bg-red-50"
                                                    >
                                                        Stop Repeating
                                                    </button>
                                                )}
                                            </div>
                                        </div>
                                    );
                                })}
                            </div>
                        )}
                    </section>
                </div>
            </div>
        </PlayerGuard>
    );
}
