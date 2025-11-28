// src/pages/admin/tabs/PlayersTab.tsx
// or: src/components/admin/PlayersTab.tsx – use the path you already have

import { useEffect, useState } from "react";
import { playersApi, type PlayerDto } from "../../../utilities/playersApi";
import { transactionsApi } from "../../../utilities/transactionsApi";
import toast from "react-hot-toast";

type BalanceMap = Record<string, number>;

/**
 * Admin Players tab:
 * - shows all players (DTOs, not EF entities)
 * - shows their current balance
 * - allows creating new players
 * - allows toggling active/inactive state
 */
export default function PlayersTab() {
    // We use PlayerDto from the API, not the old Player EF entity
    const [players, setPlayers] = useState<PlayerDto[]>([]);
    const [balances, setBalances] = useState<BalanceMap>({});
    const [isLoading, setIsLoading] = useState(true);

    // Form state for "Add Player"
    const [isFormOpen, setIsFormOpen] = useState(false);
    const [fullName, setFullName] = useState("");
    const [phone, setPhone] = useState("");
    const [email, setEmail] = useState("");
    const [active, setActive] = useState(false);
    const [isSaving, setIsSaving] = useState(false);

    // Reset form fields to default values
    const resetForm = () => {
        setFullName("");
        setPhone("");
        setEmail("");
        setActive(false);
    };

    // Load players and their balances from the server
    const loadData = async () => {
        try {
            setIsLoading(true);

            // Get all players (null → no filter by isActive)
            const list = await playersApi.getPlayers(null);
            setPlayers(list);

            // For each player, load their balance (DTO with balance number)
            const entries = await Promise.all(
                list.map(async (p) => {
                    try {
                        const balanceDto =
                            await transactionsApi.getPlayerBalance(p.id);
                        return [p.id, balanceDto.balance] as const;
                    } catch {
                        // If balance fails to load, default to 0
                        return [p.id, 0] as const;
                    }
                })
            );

            setBalances(Object.fromEntries(entries));
        } catch (err) {
            console.error(err);
            toast.error("Failed to load players");
        } finally {
            setIsLoading(false);
        }
    };

    // Initial load on component mount
    useEffect(() => {
        void loadData();
    }, []);

    // Toggle active/inactive status for a player
    const toggleActive = async (player: PlayerDto) => {
        try {
            if (player.isActive) {
                await playersApi.deactivatePlayer(player.id);
                toast.success("Player deactivated");
            } else {
                await playersApi.activatePlayer(player.id);
                toast.success("Player activated");
            }
            await loadData();
        } catch (err) {
            console.error(err);
            toast.error("Failed to update player status");
        }
    };

    // Create a new player from the form
    const handleCreatePlayer = async (e: React.FormEvent) => {
        e.preventDefault();

        // Basic client-side validation: all fields required
        if (!fullName.trim() || !phone.trim() || !email.trim()) {
            toast.error("Please fill in all fields");
            return;
        }

        try {
            setIsSaving(true);

            const dto = {
                fullName,
                phone,
                email,
            };

            // Server returns PlayerResponseDto (alias PlayerDto)
            const player = await playersApi.createPlayer(dto);

            // Optionally activate immediately if checkbox is set
            if (active) {
                try {
                    await playersApi.activatePlayer(player.id);
                } catch (err) {
                    console.error(err);
                    toast.error("Player created, but activating failed");
                }
            }

            toast.success("Player created");
            resetForm();
            setIsFormOpen(false);
            await loadData();
        } catch (err) {
            console.error(err);
            toast.error("Failed to create player");
        } finally {
            setIsSaving(false);
        }
    };

    // Toggle form open/close and reset when opening
    const handleOpenForm = () => {
        if (!isFormOpen) {
            resetForm();
        }
        setIsFormOpen((prev) => !prev);
    };

    return (
        <section className="bg-white rounded-2xl shadow-sm border border-slate-200 p-6">
            <div className="flex items-center justify-between mb-4">
                <div>
                    <h2 className="text-lg font-semibold text-slate-900">
                        Player Management
                    </h2>
                    <p className="text-xs text-slate-500">
                        Manage all registered players
                    </p>
                </div>

                <button
                    type="button"
                    onClick={handleOpenForm}
                    className="inline-flex items-center rounded-full bg-slate-900 px-4 py-1.5 text-xs font-semibold text-white hover:bg-slate-800"
                >
                    {isFormOpen ? "Close" : "Add Player"}
                </button>
            </div>

            {/* Create Player form */}
            {isFormOpen && (
                <form
                    onSubmit={handleCreatePlayer}
                    className="mb-6 rounded-2xl bg-slate-50 p-4 grid gap-4 md:grid-cols-2 xl:grid-cols-4"
                >
                    <div className="md:col-span-2">
                        <label className="block text-xs font-semibold text-slate-700 mb-1">
                            Full Name
                        </label>
                        <input
                            type="text"
                            className="input input-bordered w-full text-sm bg-white"
                            placeholder="John Doe"
                            value={fullName}
                            onChange={(e) => setFullName(e.target.value)}
                        />
                    </div>

                    <div>
                        <label className="block text-xs font-semibold text-slate-700 mb-1">
                            Phone Number
                        </label>
                        <input
                            type="text"
                            className="input input-bordered w-full text-sm bg-white"
                            placeholder="+45 12 34 56 78"
                            value={phone}
                            onChange={(e) => setPhone(e.target.value)}
                        />
                    </div>

                    <div>
                        <label className="block text-xs font-semibold text-slate-700 mb-1">
                            Email
                        </label>
                        <input
                            type="email"
                            className="input input-bordered w-full text-sm bg-white"
                            placeholder="john@example.com"
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                        />
                    </div>

                    <div className="flex items-center gap-3 md:col-span-2 xl:col-span-1">
                        <input
                            type="checkbox"
                            className="toggle toggle-sm"
                            checked={active}
                            onChange={(e) => setActive(e.target.checked)}
                        />
                        <span className="text-xs text-slate-700">
                            Active (can participate in games)
                        </span>
                    </div>

                    <div className="flex justify-end gap-2 md:col-span-2 xl:col-span-4">
                        <button
                            type="button"
                            onClick={() => {
                                resetForm();
                                setIsFormOpen(false);
                            }}
                            className="rounded-full border border-slate-300 px-4 py-1.5 text-xs font-semibold text-slate-700 hover:bg-slate-100"
                        >
                            Cancel
                        </button>
                        <button
                            type="submit"
                            disabled={isSaving}
                            className="rounded-full bg-slate-900 px-5 py-1.5 text-xs font-semibold text-white hover:bg-slate-800 disabled:opacity-60"
                        >
                            {isSaving ? "Saving..." : "Save"}
                        </button>
                    </div>
                </form>
            )}

            {/* Players table */}
            {isLoading ? (
                <p className="text-sm text-slate-500">Loading players...</p>
            ) : players.length === 0 ? (
                <p className="text-sm text-slate-500">
                    No players yet. Add your first player to get started.
                </p>
            ) : (
                <div className="overflow-x-auto">
                    <table className="min-w-full text-sm">
                        <thead>
                        <tr className="border-b border-slate-200 text-left text-xs uppercase text-slate-500">
                            <th className="py-2 pr-4">Name</th>
                            <th className="py-2 pr-4">Phone</th>
                            <th className="py-2 pr-4">Email</th>
                            <th className="py-2 pr-4">Balance</th>
                            <th className="py-2 pr-4">Status</th>
                            <th className="py-2 pr-4">Joined</th>
                            <th className="py-2 pr-4 text-right">
                                Actions
                            </th>
                        </tr>
                        </thead>
                        <tbody>
                        {players.map((p) => {
                            const balance = balances[p.id] ?? 0;
                            const created =
                                p.createdAt &&
                                new Date(p.createdAt).toLocaleDateString();

                            return (
                                <tr
                                    key={p.id}
                                    className="border-b border-slate-100 last:border-0"
                                >
                                    <td className="py-3 pr-4 font-medium text-slate-900">
                                        {p.fullName}
                                    </td>
                                    <td className="py-3 pr-4 text-slate-700">
                                        {p.phone}
                                    </td>
                                    <td className="py-3 pr-4 text-slate-700">
                                        {p.email}
                                    </td>
                                    <td className="py-3 pr-4 text-slate-900">
                                        {balance.toFixed(2)} DKK
                                    </td>
                                    <td className="py-3 pr-4">
                                        {p.isActive ? (
                                            <span className="inline-flex items-center rounded-full bg-green-100 px-3 py-1 text-xs font-semibold text-green-700">
                                                    Active
                                                </span>
                                        ) : (
                                            <span className="inline-flex items-center rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">
                                                    Inactive
                                                </span>
                                        )}
                                    </td>
                                    <td className="py-3 pr-4 text-slate-500">
                                        {created ?? "–"}
                                    </td>
                                    <td className="py-3 pl-4 text-right">
                                        <button
                                            type="button"
                                            onClick={() =>
                                                toggleActive(p)
                                            }
                                            className={
                                                "ml-2 inline-flex items-center rounded-full border px-3 py-1 text-xs font-semibold " +
                                                (p.isActive
                                                    ? "border-red-300 text-red-700 hover:bg-red-50"
                                                    : "border-green-300 text-green-700 hover:bg-green-50")
                                            }
                                        >
                                            {p.isActive
                                                ? "Deactivate"
                                                : "Activate"}
                                        </button>
                                    </td>
                                </tr>
                            );
                        })}
                        </tbody>
                    </table>
                </div>
            )}
        </section>
    );
}
