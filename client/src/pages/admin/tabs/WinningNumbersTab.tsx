// src/components/admin/WinningNumbersTab.tsx
import { useEffect, useState, type FormEvent } from "react";
import { gamesApi } from "../../../utilities/gamesApi";
import type { Game } from "../../../core/generated-client";
import toast from "react-hot-toast";

export default function WinningNumbersTab() {
    const [activeGame, setActiveGame] = useState<Game | null>(null);
    const [n1, setN1] = useState<number | "">("");
    const [n2, setN2] = useState<number | "">("");
    const [n3, setN3] = useState<number | "">("");
    const [isSaving, setIsSaving] = useState(false);

    const loadActiveGame = async () => {
        try {
            const game = await gamesApi.getActiveGame();
            setActiveGame(game);
        } catch (err) {
            console.error(err);
            toast.error("Failed to load active game");
        }
    };

    useEffect(() => {
        void loadActiveGame();
    }, []);

    const numbers = Array.from({ length: 16 }, (_, i) => i + 1);

    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();

        if (n1 === "" || n2 === "" || n3 === "") {
            toast.error("Please select all 3 winning numbers");
            return;
        }

        const chosen = [n1, n2, n3];
        const uniqueCount = new Set(chosen).size;

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
            await gamesApi.setWinningNumbers({
                gameId: activeGame.id,
                winningNumbers: chosen,
            });
            toast.success("Winning numbers saved. Game closed.");
            await loadActiveGame();
        } catch (err) {
            console.error(err);
            toast.error("Failed to save winning numbers");
        } finally {
            setIsSaving(false);
        }
    };

    const weekLabel =
        activeGame &&
        `Week ${activeGame.weeknumber}, ${activeGame.year.toString()}`;

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
