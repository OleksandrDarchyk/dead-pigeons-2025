// client/src/hooks/usePlayerBoards.ts
import { useEffect, useState } from "react";
import { boardsApi } from "../utilities/boardsApi";
import { gamesApi } from "../utilities/gamesApi";
import type {
    BoardResponseDto,
    GameResponseDto,
} from "../core/generated-client";
import toast from "react-hot-toast";

type BoardFormState = {
    selectedNumbers: number[];
};

// Simple price table based on how many numbers are picked
function getPriceForCount(count: number): number | null {
    switch (count) {
        case 5:
            return 20;
        case 6:
            return 40;
        case 7:
            return 80;
        case 8:
            return 160;
        default:
            return null;
    }
}

/**
 * enabled = false → the hook is prepared but does not fire requests yet
 * (for example, until we know the user is logged in as a player).
 */
export function usePlayerBoards(enabled: boolean = true) {
    // Boards for the current logged-in player (DTOs, not raw EF entities)
    const [boards, setBoards] = useState<BoardResponseDto[]>([]);

    // Active game (DTO)
    const [activeGame, setActiveGame] = useState<GameResponseDto | null>(null);

    const [isLoading, setIsLoading] = useState(true);
    const [isSaving, setIsSaving] = useState(false);

    const [form, setForm] = useState<BoardFormState>({
        selectedNumbers: [],
    });

    // Load active game and current player's boards
    const loadBoards = async () => {
        try {
            setIsLoading(true);

            const [game, myBoards] = await Promise.all([
                gamesApi.getActiveGame(),
                boardsApi.getMyBoards(),
            ]);

            setActiveGame(game);
            setBoards(Array.isArray(myBoards) ? myBoards : []);
        } catch (err) {
            console.error(err);
            toast.error("Failed to load your boards");
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => {
        // If hook is disabled (for example, user is not ready yet), skip loading
        if (!enabled) {
            return;
        }

        void loadBoards();
    }, [enabled]);

    // Toggle a number in the board form
    const toggleNumber = (num: number) => {
        setForm((prev) => {
            const alreadySelected = prev.selectedNumbers.includes(num);

            if (alreadySelected) {
                const updated = prev.selectedNumbers.filter((n) => n !== num);
                return { ...prev, selectedNumbers: updated };
            }

            if (prev.selectedNumbers.length >= 8) {
                toast.error("You can pick at most 8 numbers");
                return prev;
            }

            const updated = [...prev.selectedNumbers, num].sort((a, b) => a - b);
            return { ...prev, selectedNumbers: updated };
        });
    };

    // Create a new board for the active game
    const submitBoard = async () => {
        const count = form.selectedNumbers.length;

        // Basic client-side validation for UX
        if (count < 5 || count > 8) {
            toast.error("You must pick between 5 and 8 numbers");
            return;
        }

        if (!activeGame) {
            toast.error("No active game available");
            return;
        }

        const price = getPriceForCount(count);
        if (price === null) {
            toast.error("Could not calculate board price");
            return;
        }

        try {
            setIsSaving(true);

            // Server will validate gameId, numbers, repeatWeeks and balance
            await boardsApi.createBoard({
                gameId: activeGame.id,
                numbers: form.selectedNumbers,
                repeatWeeks: 0, // 0 = only this week's game
            });

            toast.success("Board created");

            // Reset form after successful creation
            setForm({
                selectedNumbers: [],
            });

            // Reload boards to reflect the new purchase
            await loadBoards();
        } catch (err) {
            console.error(err);
            toast.error("Failed to create board");
        } finally {
            setIsSaving(false);
        }
    };

    // Current price preview for UI, based on how many numbers are selected
    const currentPrice = getPriceForCount(form.selectedNumbers.length);

    return {
        boards,
        activeGame,
        isLoading,
        isSaving,
        form,
        setForm,
        toggleNumber,
        submitBoard,
        currentPrice,
    };
}
