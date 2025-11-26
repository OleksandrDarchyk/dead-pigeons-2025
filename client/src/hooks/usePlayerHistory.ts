// client/src/hooks/usePlayerHistory.ts
import { useEffect, useState } from "react";
import { gamesApi } from "../utilities/gamesApi";
import { boardsApi } from "../utilities/boardsApi";
import type { Board, Game } from "../core/generated-client";
import toast from "react-hot-toast";

export type GameHistoryItem = {
    game: Game;
    boards: Board[];
};

export function usePlayerHistory(enabled: boolean = true) {
    const [items, setItems] = useState<GameHistoryItem[]>([]);
    const [isLoading, setIsLoading] = useState(enabled);

    useEffect(() => {
        if (!enabled) {
            setIsLoading(false);
            setItems([]);
            return;
        }

        const load = async () => {
            try {
                setIsLoading(true);

                // історія всіх ігор
                const games = await gamesApi.getGamesHistory();
                // всі борди поточного гравця
                const myBoards = await boardsApi.getMyBoards();

                // новіші роки/тижні вище
                const sortedGames = [...games].sort((a, b) => {
                    if (a.year !== b.year) return b.year - a.year;
                    return b.weeknumber - a.weeknumber;
                });

                const grouped: GameHistoryItem[] = sortedGames
                    .map((game) => {
                        const boardsForGame = myBoards.filter(
                            (board) =>
                                board.gameid === game.id && !board.deletedat
                        );

                        return { game, boards: boardsForGame };
                    })
                    // показуємо тільки ігри, де у гравця є борди
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
