// client/src/hooks/usePlayerHistory.ts
import { useEffect, useState } from "react";
import { gamesApi } from "../utilities/gamesApi";
import { boardsApi } from "../utilities/boardsApi";
import type {
    BoardResponseDto,
    GameResponseDto,
} from "../core/generated-client";
import toast from "react-hot-toast";

export type GameHistoryItem = {
    // Game info for one week
    game: GameResponseDto;
    // All boards the current player has in that game
    boards: BoardResponseDto[];
};

/**
 * enabled = false → the hook is prepared but does not load anything yet
 * (for example, when user is not logged in or role is not known).
 */
export function usePlayerHistory(enabled: boolean = true) {
    const [items, setItems] = useState<GameHistoryItem[]>([]);
    const [isLoading, setIsLoading] = useState(enabled);

    useEffect(() => {
        if (!enabled) {
            // When disabled, we clear data and mark as not loading
            setIsLoading(false);
            setItems([]);
            return;
        }

        const load = async () => {
            try {
                setIsLoading(true);

                // Full games history (DTOs)
                const games = await gamesApi.getGamesHistory();

                // All boards for the current player (DTOs)
                const myBoards = await boardsApi.getMyBoards();

                // Newest games first by year, then by weekNumber
                const sortedGames = [...games].sort((a, b) => {
                    if (a.year !== b.year) return b.year - a.year;
                    return b.weekNumber - a.weekNumber;
                });

                const grouped: GameHistoryItem[] = sortedGames
                    .map((game) => {
                        // Filter only boards that belong to this game
                        const boardsForGame = myBoards.filter(
                            (board) => board.gameId === game.id
                        );

                        return { game, boards: boardsForGame };
                    })
                    // Show only games where player actually has boards
                    .filter((item) => item.boards.length > 0);

                setItems(grouped);
            } catch (err) {
                console.error(err);
                toast.error("Failed to load games history");
                setItems([]);
            } finally {
                setIsLoading(false);
            }
        };

        void load();
    }, [enabled]);

    return {
        items,
        isLoading,
    };
}
