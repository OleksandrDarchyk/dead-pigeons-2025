import { useEffect, useState } from "react";
import toast from "react-hot-toast";

import { gamesApi } from "@core/api/gamesApi";
import type { GameResponseDto, PlayerGameHistoryItemDto } from "@core/api/generated/generated-client";
import { ApiException } from "@core/api/generated/generated-client";


export type GameHistoryItem = {
    game: GameResponseDto;
    boards: PlayerGameHistoryItemDto[];
};

export function usePlayerHistory(enabled: boolean = true) {
    const [items, setItems] = useState<GameHistoryItem[]>([]);
    const [isLoading, setIsLoading] = useState(enabled);
    const [isNotPlayer, setIsNotPlayer] = useState(false);

    useEffect(() => {
        if (!enabled) {
            setIsLoading(false);
            setItems([]);
            setIsNotPlayer(false);
            return;
        }

        const load = async () => {
            try {
                setIsLoading(true);
                setIsNotPlayer(false);

                const history: PlayerGameHistoryItemDto[] =
                    await gamesApi.getMyGameHistory();

                const groupedByGame = new Map<string, GameHistoryItem>();

                for (const entry of history ?? []) {
                    const gameId = entry.gameId ?? "";
                    if (!gameId) continue;

                    let existing = groupedByGame.get(gameId);

                    if (!existing) {
                        const game: GameResponseDto = {
                            id: entry.gameId,
                            weekNumber: entry.weekNumber ?? 0,
                            year: entry.year ?? 0,
                            winningNumbers:
                                entry.winningNumbers ?? undefined,
                            isActive: false,
                            createdAt: undefined,
                            closedAt:
                                entry.gameClosedAt ?? undefined,
                        };

                        existing = { game, boards: [] };
                        groupedByGame.set(gameId, existing);
                    }

                    existing.boards.push(entry);
                }

                const groupedArray: GameHistoryItem[] = Array.from(
                    groupedByGame.values()
                ).sort((a, b) => {
                    const yearDiff =
                        (b.game.year ?? 0) - (a.game.year ?? 0);
                    if (yearDiff !== 0) return yearDiff;

                    return (
                        (b.game.weekNumber ?? 0) -
                        (a.game.weekNumber ?? 0)
                    );
                });

                setItems(groupedArray);
            } catch (err) {
                console.error(err);

                if (err instanceof ApiException) {
                    let problemTitle: string | undefined;

                    try {
                        if (err.response) {
                            const parsed = JSON.parse(err.response) as {
                                title?: string;
                                detail?: string;
                            };
                            problemTitle =
                                parsed?.title ?? parsed?.detail;
                        }
                    } catch {

                    }

                    if (
                        problemTitle &&
                        problemTitle.includes(
                            "Player not found for the current user.",
                        )
                    ) {
                        setItems([]);
                        setIsNotPlayer(true);
                        setIsLoading(false);
                        return;
                    }
                }

                setIsNotPlayer(false);
                setItems([]);
                toast.error("Failed to load game history");
            } finally {
                setIsLoading(false);
            }
        };

        void load();
    }, [enabled]);

    return {
        items,
        isLoading,
        isNotPlayer,
    };
}
