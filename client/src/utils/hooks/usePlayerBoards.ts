import { useEffect, useState } from "react";
import { boardsApi } from "@core/api/boardsApi";
import { gamesApi } from "@core/api/gamesApi";
import type { BoardResponseDto, GameResponseDto } from "@core/api/generated/generated-client";
import toast from "react-hot-toast";


type BoardFormState = {
    selectedNumbers: number[];
    repeatEnabled: boolean;
    repeatWeeks: number;
};

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

export function usePlayerBoards(enabled: boolean = true) {
    const [boards, setBoards] = useState<BoardResponseDto[]>([]);

    const [activeGame, setActiveGame] = useState<GameResponseDto | null>(null);

    const [isLoading, setIsLoading] = useState<boolean>(enabled);
    const [isSaving, setIsSaving] = useState(false);

    const [form, setForm] = useState<BoardFormState>({
        selectedNumbers: [],
        repeatEnabled: false,
        repeatWeeks: 1,
    });

    const loadBoards = async () => {
        if (!enabled) return;

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
        if (!enabled) {
            setIsLoading(false);
            setBoards([]);
            setActiveGame(null);
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

    const setRepeatEnabled = (enabledFlag: boolean) => {
        setForm((prev) => ({
            ...prev,
            repeatEnabled: enabledFlag,
        }));
    };

    const setRepeatWeeks = (weeks: number) => {
        const raw = Number.isFinite(weeks) ? weeks : 1;
        const clamped = Math.max(1, Math.min(52, Math.floor(raw))); // 1–52 weeks
        setForm((prev) => ({
            ...prev,
            repeatWeeks: clamped,
        }));
    };

    const submitBoard = async () => {
        const count = form.selectedNumbers.length;

        if (count < 5 || count > 8) {
            toast.error("You must pick between 5 and 8 numbers");
            return;
        }

        if (!activeGame) {
            toast.error("No active game available");
            return;
        }

        if (form.repeatEnabled && (form.repeatWeeks < 1 || form.repeatWeeks > 52)) {
            toast.error("Repeat weeks must be between 1 and 52");
            return;
        }

        const price = getPriceForCount(count);
        if (price === null) {
            toast.error("Could not calculate board price");
            return;
        }

        const repeatWeeksToSend = form.repeatEnabled ? form.repeatWeeks : 0;

        try {
            setIsSaving(true);

            await boardsApi.createBoard({
                gameId: activeGame.id,
                numbers: form.selectedNumbers,
                repeatWeeks: repeatWeeksToSend,
            });

            toast.success("Board created");

            setForm({
                selectedNumbers: [],
                repeatEnabled: false,
                repeatWeeks: 1,
            });

            await loadBoards();
        } catch (err) {
            console.error(err);
            toast.error("Failed to create board");
        } finally {
            setIsSaving(false);
        }
    };

    const stopRepeating = async (boardId: string) => {
        try {
            setIsSaving(true);

            const updated = await boardsApi.stopRepeatingMyBoard({ boardId });

            setBoards((prev) =>
                prev.map((b) => (b.id === updated.id ? updated : b)),
            );

            toast.success("Repeating disabled for this board");
        } catch (err) {
            console.error(err);
            toast.error("Failed to stop repeating this board");
        } finally {
            setIsSaving(false);
        }
    };

    const currentPrice = getPriceForCount(form.selectedNumbers.length);
    const totalPrice =
        currentPrice !== null
            ? currentPrice * (form.repeatEnabled ? form.repeatWeeks : 1)
            : null;

    return {
        boards,
        activeGame,
        isLoading,
        isLoadingBoards: isLoading,
        isSaving,
        form,
        setForm,
        toggleNumber,
        submitBoard,
        currentPrice,
        totalPrice,
        setRepeatEnabled,
        setRepeatWeeks,
        stopRepeating,
        reloadBoards: loadBoards,
    };
}
