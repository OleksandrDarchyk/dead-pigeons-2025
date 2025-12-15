// client/src/components/admin/BoardsStatsTab.tsx
import { useEffect, useMemo, useState } from "react";
import type {
    BoardResponseDto,
    GameResponseDto,
    PlayerResponseDto,
} from "@core/api/generated/generated-client";
import { gamesApi } from "@core/api/gamesApi";
import { boardsApi } from "@core/api/boardsApi";
import { playersApi } from "@core/api/playersApi";
import toast from "react-hot-toast";



// Boards are safe DTOs coming from the API
type BoardWithPlayer = BoardResponseDto;

export default function BoardsStatsTab() {
    // All games history (active + finished + scheduled)
    const [games, setGames] = useState<GameResponseDto[]>([]);

    // Selected game id for the dropdown (empty string means "nothing selected yet")
    const [selectedGameId, setSelectedGameId] = useState<string>("");

    // Currently selected game (derived from games + selectedGameId)
    const selectedGame: GameResponseDto | null =
        games.find((g) => g.id === selectedGameId) ?? null;

    // Boards for the selected game
    const [boards, setBoards] = useState<BoardWithPlayer[]>([]);

    // Active players as DTOs (for resolving names)
    const [players, setPlayers] = useState<PlayerResponseDto[]>([]);

    const [isLoadingGames, setIsLoadingGames] = useState(true);
    const [isLoadingBoards, setIsLoadingBoards] = useState(false);

    // Load games history and active players once
    useEffect(() => {
        const loadGamesAndPlayers = async () => {
            try {
                setIsLoadingGames(true);

                // Load all games (history)
                const allGames = await gamesApi.getGamesHistory();
                setGames(Array.isArray(allGames) ? allGames : []);

                // Pick initial game:
                // Prefer the active game, otherwise the first in the list
                if (allGames.length > 0) {
                    const active = allGames.find((g) => g.isActive);
                    const initial = active ?? allGames[0];
                    setSelectedGameId(initial.id);
                }

                // Load active players for name resolution
                const allPlayers = await playersApi.getPlayers(true);
                setPlayers(Array.isArray(allPlayers) ? allPlayers : []);
            } catch (err) {
                console.error(err);
                toast.error("Failed to load games and players");
            } finally {
                setIsLoadingGames(false);
            }
        };

        // Explicitly ignore the Promise in effect
        void loadGamesAndPlayers();
    }, []);

    // Load boards when selected game changes
    useEffect(() => {
        if (!selectedGameId) {
            setBoards([]);
            return;
        }

        const loadBoardsForGame = async () => {
            try {
                setIsLoadingBoards(true);
                const boardsForGame = await boardsApi.getBoardsForGame(
                    selectedGameId,
                );
                setBoards(
                    Array.isArray(boardsForGame) ? boardsForGame : [],
                );
            } catch (err) {
                console.error(err);
                toast.error("Failed to load boards for selected game");
                setBoards([]);
            } finally {
                setIsLoadingBoards(false);
            }
        };

        // Explicitly ignore the Promise in effect
        void loadBoardsForGame();
    }, [selectedGameId]);

    // High-level statistics for the selected game
    const stats = useMemo(() => {
        if (!selectedGame) {
            return {
                totalPlayersInGame: 0,
                activeBoards: 0,
                repeatingBoards: 0,
                winningBoards: 0,
                digitalRevenue: 0,
            };
        }

        // Unique players who actually bought at least one board in this game
        const playersInGame = new Set(boards.map((b) => b.playerId));
        const totalPlayersInGame = playersInGame.size;

        const activeBoards = boards.length;
        const repeatingBoards = boards.filter((b) => b.repeatActive)
            .length;
        const winningBoards = boards.filter((b) => b.isWinning).length;

        // Sum of board prices for this game
        const digitalRevenue = boards.reduce(
            (sum, b) => sum + (b.price ?? 0),
            0,
        );

        return {
            totalPlayersInGame,
            activeBoards,
            repeatingBoards,
            winningBoards,
            digitalRevenue,
        };
    }, [boards, selectedGame]);

    // Resolve player name for a board using playerId from DTO and PlayerResponseDto list
    const getPlayerName = (board: BoardWithPlayer): string => {
        const player = players.find((p) => p.id === board.playerId);
        return player?.fullName ?? "Unknown player";
    };

    // Human-friendly label for selected game
    const weekLabel =
        selectedGame &&
        `Week ${selectedGame.weekNumber}, ${selectedGame.year?.toString()}`;

    const gameStatus =
        selectedGame &&
        (selectedGame.isActive
            ? "Active"
            : selectedGame.closedAt
                ? "Finished"
                : "Scheduled");

    const isLoading = isLoadingGames || isLoadingBoards;

    return (
        <section className="space-y-6">
            {/* Top statistics card for the selected game */}
            <div className="bg-white rounded-2xl shadow-sm border border-slate-200 p-6">
                <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
                    <div>
                        <h2 className="text-lg font-semibold text-slate-900 mb-1">
                            Statistics Overview
                        </h2>
                        <p className="text-xs text-slate-500">
                            Select a week to see all boards and results.
                        </p>
                    </div>

                    {/* Game selector */}
                    <div className="flex flex-col items-start gap-1 text-xs">
                        <span className="font-semibold text-slate-700">
                            Game:
                        </span>
                        <select
                            className="select select-bordered w-full md:w-64 text-sm bg-slate-50"
                            value={selectedGameId}
                            onChange={(e) =>
                                setSelectedGameId(e.target.value)
                            }
                            disabled={
                                isLoadingGames || games.length === 0
                            }
                        >
                            {games.length === 0 && (
                                <option value="">No games found</option>
                            )}

                            {games.map((g) => (
                                <option key={g.id} value={g.id}>
                                    Week {g.weekNumber}, {g.year}{" "}
                                    {g.isActive
                                        ? "(Active)"
                                        : g.closedAt
                                            ? "(Finished)"
                                            : "(Scheduled)"}
                                </option>
                            ))}
                        </select>
                    </div>
                </div>

                <div className="mt-4">
                    {isLoading && !selectedGame ? (
                        <p className="text-sm text-slate-500">
                            Loading stats...
                        </p>
                    ) : !selectedGame ? (
                        <p className="text-sm text-slate-500">
                            No game selected.
                        </p>
                    ) : (
                        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-4">
                            {/* Selected game info */}
                            <div className="rounded-xl bg-slate-50 px-4 py-3">
                                <p className="text-xs text-slate-500 mb-1">
                                    Selected game
                                </p>
                                <p className="text-sm font-bold text-slate-900">
                                    {weekLabel}
                                </p>
                                <p className="text-[11px] text-slate-500 mt-1">
                                    Status: {gameStatus}
                                </p>
                            </div>

                            {/* Total unique players in this game */}
                            <div className="rounded-xl bg-slate-50 px-4 py-3">
                                <p className="text-xs text-slate-500 mb-1">
                                    Players in this game
                                </p>
                                <p className="text-2xl font-bold text-slate-900">
                                    {stats.totalPlayersInGame}
                                </p>
                            </div>

                            {/* Total number of boards */}
                            <div className="rounded-xl bg-green-50 px-4 py-3">
                                <p className="text-xs text-slate-500 mb-1">
                                    Total boards
                                </p>
                                <p className="text-2xl font-bold text-green-800">
                                    {stats.activeBoards}
                                </p>
                            </div>

                            {/* Winning boards */}
                            <div className="rounded-xl bg-emerald-50 px-4 py-3">
                                <p className="text-xs text-emerald-700 mb-1">
                                    Winning boards
                                </p>
                                <p className="text-2xl font-bold text-emerald-900">
                                    {stats.winningBoards}
                                </p>
                                <p className="mt-0.5 text-[11px] text-emerald-700">
                                    Boards that matched all 3 winning
                                    numbers.
                                </p>
                            </div>

                            {/* Digital revenue */}
                            <div className="rounded-xl bg-amber-50 px-4 py-3">
                                <p className="text-xs text-slate-500 mb-1">
                                    Digital revenue
                                </p>
                                <p className="text-2xl font-bold text-amber-800">
                                    {stats.digitalRevenue}{" "}
                                    <span className="text-sm font-semibold">
                                        DKK
                                    </span>
                                </p>
                                <p className="mt-0.5 text-[11px] text-slate-500">
                                    Before 70/30 split
                                </p>
                            </div>
                        </div>
                    )}
                </div>
            </div>

            {/* List of boards for the selected game */}
            <div className="bg-white rounded-2xl shadow-sm border border-slate-200 p-6">
                <h3 className="text-base font-semibold text-slate-900 mb-1">
                    Boards for selected game
                </h3>
                <p className="text-xs text-slate-500 mb-4">
                    View all purchased boards, including winners.
                </p>

                {isLoading ? (
                    <p className="text-sm text-slate-500">
                        Loading boards...
                    </p>
                ) : !selectedGame ? (
                    <p className="text-sm text-slate-500">
                        Select a game to see its boards.
                    </p>
                ) : boards.length === 0 ? (
                    <p className="text-sm text-slate-500">
                        No boards for this game.
                    </p>
                ) : (
                    <div className="space-y-3">
                        {boards.map((b) => {
                            const playerName = getPlayerName(b);
                            const created =
                                b.createdAt &&
                                new Date(
                                    b.createdAt,
                                ).toLocaleDateString();

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
                                                    Winner
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
