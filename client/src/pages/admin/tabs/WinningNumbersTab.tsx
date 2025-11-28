// src/components/admin/WinningNumbersTab.tsx
import { useEffect, useState, type FormEvent } from "react";
import { gamesApi } from "../../../utilities/gamesApi";
import type { GameResponseDto } from "../../../core/generated-client";
import toast from "react-hot-toast";

export default function WinningNumbersTab() {
    // Active game is loaded from the API as GameResponseDto
    const [activeGame, setActiveGame] = useState<GameResponseDto | null>(null);

    // Local state for 3 winning numbers (can be number or empty string when not selected)
    const [n1, setN1] = useState<number | "">("");
    const [n2, setN2] = useState<number | "">("");
    const [n3, setN3] = useState<number | "">("");
    const [isSaving, setIsSaving] = useState(false);

    // Load currently active game from the server
    const loadActiveGame = async () => {
        try {
            const game = await gamesApi.getActiveGame();
            setActiveGame(game);
        } catch (err) {
            console.error(err);
            toast.error("Failed to load active game");
        }
    };

    // Load game on first render
    useEffect(() => {
        void loadActiveGame();
    }, []);

    // Available numbers 1–16
    const numbers = Array.from({ length: 16 }, (_, i) => i + 1);

    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();

        // Basic client-side validation: all 3 numbers must be selected
        if (n1 === "" || n2 === "" || n3 === "") {
            toast.error("Please select all 3 winning numbers");
            return;
        }

        const chosen = [n1, n2, n3];
        const uniqueCount = new Set(chosen).size;

        // All three must be distinct
        if (uniqueCount !== 3) {
            toast.error("Winning numbers must be different");
            return;
        }

        if (!activeGame) {
            toast.error("No active game found");
            return;
        }

        try {
            setIsSaving(true);

            // Call API to set winning numbers for the active game
            await gamesApi.setWinningNumbers({
                gameId: activeGame.id,
                winningNumbers: chosen,
            });

            toast.success("Winning numbers saved. Game closed.");

            // Reload active game (will move to the next game if available)
            await loadActiveGame();
        } catch (err) {
            console.error(err);
            toast.error("Failed to save winning numbers");
        } finally {
            setIsSaving(false);
        }
    };

    // Human-readable label for the current active game
    const weekLabel =
        activeGame &&
        `Week ${activeGame.weekNumber}, ${activeGame.year.toString()}`;

    return (
        <section className="bg-white rounded-2xl shadow-sm border border-slate-200 p-6">
            <h2 className="text-lg font-semibold text-slate-900 mb-1">
                Enter Winning Numbers
            </h2>
            <p className="text-xs text-slate-500 mb-4">
                {weekLabel ?? "Loading current game..."}
            </p>

            <form onSubmit={handleSubmit} className="space-y-5">
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                    <div>
                        <label className="block text-xs font-semibold text-slate-700 mb-1">
                            First Number (1–16)
                        </label>
                        <select
                            className="select select-bordered w-full text-sm bg-slate-50"
                            value={n1 === "" ? "" : n1}
                            onChange={(e) =>
                                setN1(
                                    e.target.value === ""
                                        ? ""
                                        : Number(e.target.value)
                                )
                            }
                        >
                            <option value="">Select number</option>
                            {numbers.map((num) => (
                                <option key={num} value={num}>
                                    {num}
                                </option>
                            ))}
                        </select>
                    </div>

                    <div>
                        <label className="block text-xs font-semibold text-slate-700 mb-1">
                            Second Number (1–16)
                        </label>
                        <select
                            className="select select-bordered w-full text-sm bg-slate-50"
                            value={n2 === "" ? "" : n2}
                            onChange={(e) =>
                                setN2(
                                    e.target.value === ""
                                        ? ""
                                        : Number(e.target.value)
                                )
                            }
                        >
                            <option value="">Select number</option>
                            {numbers.map((num) => (
                                <option key={num} value={num}>
                                    {num}
                                </option>
                            ))}
                        </select>
                    </div>

                    <div>
                        <label className="block text-xs font-semibold text-slate-700 mb-1">
                            Third Number (1–16)
                        </label>
                        <select
                            className="select select-bordered w-full text-sm bg-slate-50"
                            value={n3 === "" ? "" : n3}
                            onChange={(e) =>
                                setN3(
                                    e.target.value === ""
                                        ? ""
                                        : Number(e.target.value)
                                )
                            }
                        >
                            <option value="">Select number</option>
                            {numbers.map((num) => (
                                <option key={num} value={num}>
                                    {num}
                                </option>
                            ))}
                        </select>
                    </div>
                </div>

                <button
                    type="submit"
                    disabled={isSaving}
                    className="w-full rounded-full bg-red-600 py-2.5 text-sm font-semibold text-white hover:bg-red-700 disabled:opacity-60"
                >
                    {isSaving ? "Saving..." : "Submit Winning Numbers"}
                </button>

                <p className="text-xs text-slate-400 mt-2">
                    After you submit the winning numbers, the current game is
                    closed and the next game will start automatically.
                </p>
            </form>
        </section>
    );
}
