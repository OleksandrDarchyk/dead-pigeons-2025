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
            return;
        }

        const load = async () => {
            try {
                setIsLoading(true);

                // ❗ ТУТ ВАЖЛИВО:
                // Заміни getAllGames() на правильний метод з GamesClient,
                // який повертає список усіх ігор.
                // Напр. gamesApi.getAllGames() або gamesApi.listGames() – перевір у generated-client.ts
                const games: Game[] = await (gamesApi as any).getGamesHistory();

                const myBoards = await boardsApi.getMyBoards();

                const gamesArr = Array.isArray(games) ? games : [];
                const boardsArr = Array.isArray(myBoards) ? myBoards : [];

                // новіші роки/тижні – вище
                const sortedGames = [...gamesArr].sort((a, b) => {
                    const yearA = (a as any).year ?? 0;
                    const yearB = (b as any).year ?? 0;
                    if (yearA !== yearB) return yearB - yearA;

                    const weekA = (a as any).weeknumber ?? 0;
                    const weekB = (b as any).weeknumber ?? 0;
                    return weekB - weekA;
                });

                const grouped: GameHistoryItem[] = sortedGames
                    .map((game) => {
                        const boardsForGame = boardsArr.filter(
                            (b) => b.gameid === game.id && !b.deletedat
                        );

                        return { game, boards: boardsForGame };
                    })
                    // показуємо тільки ігри, де у гравця є борди
                    .filter((item) => item.boards.length > 0);

                setItems(grouped);
            } catch (err) {
                console.error(err);
                toast.error("Failed to load games history");
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
