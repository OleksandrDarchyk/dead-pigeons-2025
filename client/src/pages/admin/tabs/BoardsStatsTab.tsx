// src/components/admin/BoardsStatsTab.tsx
import { useEffect, useMemo, useState } from "react";
import type { Board, Game, Player } from "../../../core/generated-client";
import { gamesApi } from "../../../utilities/gamesApi";
import { boardsApi } from "../../../utilities/boardsApi";
import { playersApi } from "../../../utilities/playersApi";
import toast from "react-hot-toast";


type BoardWithPlayer = Board & { player?: Player };

export default function BoardsStatsTab() {
    const [activeGame, setActiveGame] = useState<Game | null>(null);
    const [boards, setBoards] = useState<BoardWithPlayer[]>([]);
    const [players, setPlayers] = useState<Player[]>([]);
    const [isLoading, setIsLoading] = useState(true);

    const loadData = async () => {
        try {
            setIsLoading(true);

            const game = await gamesApi.getActiveGame();
            setActiveGame(game);

            const [boardsForGame, allPlayers] = await Promise.all([
                boardsApi.getBoardsForGame(game.id),
                playersApi.getPlayers(true),
            ]);

            setBoards(boardsForGame as BoardWithPlayer[]);
            setPlayers(allPlayers);
        } catch (err) {
            console.error(err);
            toast.error("Failed to load boards and stats");
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => {
        void loadData();
    }, []);

    const stats = useMemo(() => {
        const totalPlayers = players.length;
        const activeBoards = boards.filter((b) => !b.deletedat).length;
        const repeatingBoards = boards.filter((b) => b.repeatactive).length;

        const winningBoards = boards.filter((b) => b.iswinning).length;

        return {
            totalPlayers,
            activeBoards,
            repeatingBoards,
            winningBoards,
        };
    }, [boards, players]);

    const getPlayerName = (board: BoardWithPlayer) => {
        if (board.player?.fullname) return board.player.fullname;
        const player = players.find((p) => p.id === board.playerid);
        return player?.fullname ?? "Unknown player";
    };

    const weekLabel =
        activeGame &&
        `Week ${activeGame.weeknumber}, ${activeGame.year.toString()}`;

    return (
        <section className="space-y-6">
            <div className="bg-white rounded-2xl shadow-sm border border-slate-200 p-6">
                <h2 className="text-lg font-semibold text-slate-900 mb-1">
                    Statistics Overview
                </h2>
                <p className="text-xs text-slate-500 mb-4">
                    {weekLabel ?? "Current active game"}
                </p>

                {isLoading ? (
                    <p className="text-sm text-slate-500">Loading stats...</p>
                ) : (
                    <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
                        <div className="rounded-xl bg-slate-50 px-4 py-3">
                            <p className="text-xs text-slate-500 mb-1">
                                Total Players
                            </p>
                            <p className="text-2xl font-bold text-slate-900">
                                {stats.totalPlayers}
                            </p>
                        </div>
                        <div className="rounded-xl bg-green-50 px-4 py-3">
                            <p className="text-xs text-slate-500 mb-1">
                                Active Boards
                            </p>
                            <p className="text-2xl font-bold text-green-800">
                                {stats.activeBoards}
                            </p>
                        </div>
                        <div className="rounded-xl bg-violet-50 px-4 py-3">
                            <p className="text-xs text-slate-500 mb-1">
                                Repeating Boards
                            </p>
                            <p className="text-2xl font-bold text-violet-800">
                                {stats.repeatingBoards}
                            </p>
                        </div>
                        <div className="rounded-xl bg-amber-50 px-4 py-3">
                            <p className="text-xs text-slate-500 mb-1">
                                Winning Boards
                            </p>
                            <p className="text-2xl font-bold text-amber-800">
                                {stats.winningBoards}
                            </p>
                        </div>
                    </div>
                )}
            </div>

            <div className="bg-white rounded-2xl shadow-sm border border-slate-200 p-6">
                <h3 className="text-base font-semibold text-slate-900 mb-1">
                    All Active Boards
                </h3>
                <p className="text-xs text-slate-500 mb-4">
                    View all purchased boards for the current game.
                </p>

                {isLoading ? (
                    <p className="text-sm text-slate-500">Loading boards...</p>
                ) : boards.length === 0 ? (
                    <p className="text-sm text-slate-500">
                        No boards for the current game.
                    </p>
                ) : (
                    <div className="space-y-3">
                        {boards.map((b) => {
                            if (b.deletedat) return null;

                            const playerName = getPlayerName(b);
                            const created =
                                b.createdat &&
                                new Date(b.createdat).toLocaleDateString();

                            const isRepeating = b.repeatactive;
                            const isWinning = b.iswinning;

                            return (
                                <div
                                    key={b.id}
                                    className="flex flex-col md:flex-row md:items-center md:justify-between rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3"
                                >
                                    <div className="mb-2 md:mb-0">
                                        <p className="text-sm font-semibold text-slate-900">
                                            {playerName}
                                        </p>
                                        <p className="text-xs text-slate-500">
                                            {created ?? "–"}
                                        </p>
                                    </div>

                                    <div className="flex flex-wrap items-center gap-2">
                                        <div className="flex flex-wrap gap-1">
                                            {b.numbers.map((n) => (
                                                <span
                                                    key={n}
                                                    className={
                                                        "inline-flex h-7 w-7 items-center justify-center rounded-lg text-xs font-semibold " +
                                                        (isWinning
                                                            ? "bg-amber-600 text-white"
                                                            : "bg-red-600 text-white")
                                                    }
                                                >
                                                    {n}
                                                </span>
                                            ))}
                                        </div>

                                        <div className="flex items-center gap-2">
                                            {isRepeating ? (
                                                <span className="inline-flex items-center rounded-full bg-green-100 px-3 py-1 text-xs font-semibold text-green-700">
                                                Repeating
                                            </span>
                                            ) : (
                                                <span className="inline-flex items-center rounded-full bg-slate-200 px-3 py-1 text-xs font-semibold text-slate-700">
                                                Single
                                            </span>
                                            )}

                                            {isWinning && (
                                                <span className="inline-flex items-center rounded-full bg-amber-100 px-3 py-1 text-xs font-semibold text-amber-800">
                                                Winning
                                            </span>
                                            )}
                                        </div>
                                    </div>
                                </div>
                            );
                        })}
                    </div>
                )}
            </div>
        </section>
    );
}
