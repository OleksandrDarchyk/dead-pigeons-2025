// client/src/components/admin/BoardsStatsTab.tsx
import { useEffect, useMemo, useState } from "react";
import type {
    BoardResponseDto,
    GameResponseDto,
    PlayerResponseDto,
} from "../../../core/generated-client";
import { gamesApi } from "../../../utilities/gamesApi";
import { boardsApi } from "../../../utilities/boardsApi";
import { playersApi } from "../../../utilities/playersApi";
import toast from "react-hot-toast";

// Boards are safe DTOs coming from the API
type BoardWithPlayer = BoardResponseDto;

export default function BoardsStatsTab() {
    // Active game is a safe DTO (no EF entities on the client)
    const [activeGame, setActiveGame] = useState<GameResponseDto | null>(null);

    // Boards for the current active game
    const [boards, setBoards] = useState<BoardWithPlayer[]>([]);

    // Active players as DTOs
    const [players, setPlayers] = useState<PlayerResponseDto[]>([]);

    const [isLoading, setIsLoading] = useState(true);

    // Load active game, boards for that game, and active players
    const loadData = async () => {
        try {
            setIsLoading(true);

            // 1) Ask backend for the current active game
            const game = await gamesApi.getActiveGame();
            console.log("Active game:", game);

            if (!game || !game.id) {
                // No active game → clear everything
                setActiveGame(null);
                setBoards([]);
                setPlayers([]);
                return;
            }

            setActiveGame(game);

            // 2) Load boards for this game and all active players in parallel
            const [boardsForGame, allPlayers] = await Promise.all([
                boardsApi.getBoardsForGame(game.id),
                // true = only active players
                playersApi.getPlayers(true),
            ]);

            console.log("BoardsForGame:", boardsForGame);
            console.log("AllPlayers:", allPlayers);

            // Defensive checks in case backend returns something unexpected
            setBoards(Array.isArray(boardsForGame) ? boardsForGame : []);
            setPlayers(Array.isArray(allPlayers) ? allPlayers : []);
        } catch (err) {
            console.error(err);
            toast.error("Failed to load boards and stats");

            setBoards([]);
            setPlayers([]);
            setActiveGame(null);
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => {
        void loadData();
    }, []);

    // High-level statistics for the current active game
    const stats = useMemo(() => {
        // Unique players who actually bought at least one board in this game
        const playersInGame = new Set(boards.map((b) => b.playerId));
        const totalPlayersInGame = playersInGame.size;

        // Backend already returns only non-deleted boards in DTO
        const activeBoards = boards.length;
        const repeatingBoards = boards.filter((b) => b.repeatActive).length;
        const winningBoards = boards.filter((b) => b.isWinning).length;

        return {
            totalPlayersInGame,
            activeBoards,
            repeatingBoards,
            winningBoards,
        };
    }, [boards]);

    // Resolve player name for a board using playerId from DTO and PlayerResponseDto list
    const getPlayerName = (board: BoardWithPlayer): string => {
        const player = players.find((p) => p.id === board.playerId);
        return player?.fullName ?? "Unknown player";
    };

    // Human-friendly label for current active game
    const weekLabel =
        activeGame &&
        `Week ${activeGame.weekNumber}, ${activeGame.year?.toString()}`;

    return (
        <section className="space-y-6">
            {/* Top statistics card for the current active game */}
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
                        {/* How many unique players actually play in this game */}
                        <div className="rounded-xl bg-slate-50 px-4 py-3">
                            <p className="text-xs text-slate-500 mb-1">
                                Players in this game
                            </p>
                            <p className="text-2xl font-bold text-slate-900">
                                {stats.totalPlayersInGame}
                            </p>
                        </div>

                        {/* Total number of boards in this game */}
                        <div className="rounded-xl bg-green-50 px-4 py-3">
                            <p className="text-xs text-slate-500 mb-1">
                                Active Boards
                            </p>
                            <p className="text-2xl font-bold text-green-800">
                                {stats.activeBoards}
                            </p>
                        </div>

                        {/* Boards marked as repeating */}
                        <div className="rounded-xl bg-violet-50 px-4 py-3">
                            <p className="text-xs text-slate-500 mb-1">
                                Repeating Boards
                            </p>
                            <p className="text-2xl font-bold text-violet-800">
                                {stats.repeatingBoards}
                            </p>
                        </div>

                        {/* Boards marked as winning by the backend */}
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

            {/* List of boards for the current active game */}
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
                            const playerName = getPlayerName(b);
                            const created =
                                b.createdAt &&
                                new Date(b.createdAt).toLocaleDateString();

                            const isRepeating = b.repeatActive;
                            const isWinning = b.isWinning;

                            return (
                                <div
                                    key={b.id}
                                    className="flex flex-col md:flex-row md:items-center md:justify-between rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3"
                                >
                                    {/* Player info and date */}
                                    <div className="mb-2 md:mb-0">
                                        <p className="text-sm font-semibold text-slate-900">
                                            {playerName}
                                        </p>
                                        <p className="text-xs text-slate-500">
                                            {created ?? "–"}
                                        </p>
                                    </div>

                                    {/* Board numbers and small tags */}
                                    <div className="flex flex-wrap items-center gap-2">
                                        {/* Numbers on the board */}
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

                                        {/* Tags: repeating / single and winning */}
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
