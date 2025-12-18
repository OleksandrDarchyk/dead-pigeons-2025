
import {
    useEffect,
    useState,
    type FormEvent,
    useCallback,
} from "react";

import { playersApi, type PlayerDto } from "@core/api/playersApi";
import { transactionsApi } from "@core/api/transactionsApi";


import toast from "react-hot-toast";


type BalanceMap = Record<string, number>;

type StatusFilter = "all" | "active" | "inactive";
type SortBy = "fullName" | "createdAt";
type SortDirection = "asc" | "desc";

export default function PlayersTab() {
    const [players, setPlayers] = useState<PlayerDto[]>([]);
    const [balances, setBalances] = useState<BalanceMap>({});
    const [isLoading, setIsLoading] = useState(true);

    const [statusFilter, setStatusFilter] = useState<StatusFilter>("all");
    const [sortBy, setSortBy] = useState<SortBy>("fullName");
    const [sortDirection, setSortDirection] =
        useState<SortDirection>("asc");

    const [isFormOpen, setIsFormOpen] = useState(false);
    const [fullName, setFullName] = useState("");
    const [phone, setPhone] = useState("");
    const [email, setEmail] = useState("");
    const [active, setActive] = useState(false);
    const [isSaving, setIsSaving] = useState(false);

    const [editingPlayerId, setEditingPlayerId] =
        useState<string | null>(null);

    const resetForm = () => {
        setFullName("");
        setPhone("");
        setEmail("");
        setActive(false);
        setEditingPlayerId(null);
    };

    // Load players and their balances from the server
    const loadData = useCallback(async () => {
        try {
            setIsLoading(true);

            // Map statusFilter → isActive parameter for API
            const isActiveParam =
                statusFilter === "all"
                    ? null
                    : statusFilter === "active";

            // Ask backend: give me players with filter + sorting
            const list = await playersApi.getPlayers(
                isActiveParam,
                sortBy,
                sortDirection
            );
            setPlayers(list);

            // For each player, load their balance (DTO with "balance" number)
            const entries = await Promise.all(
                list.map(async (p) => {
                    try {
                        const balanceDto =
                            await transactionsApi.getPlayerBalance(
                                p.id
                            );
                        return [p.id, balanceDto.balance] as const;
                    } catch {
                        // If balance fails to load, default to 0
                        return [p.id, 0] as const;
                    }
                })
            );

            setBalances(Object.fromEntries(entries));
        } catch (err: unknown) {
            console.error(err);
            toast.error("Failed to load players");
        } finally {
            setIsLoading(false);
        }
    }, [statusFilter, sortBy, sortDirection]);

    // Initial load + reload when filters / sorting change
    useEffect(() => {
        // Explicitly ignore the Promise from loadData in this effect
        void loadData();
    }, [loadData]);

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
        } catch (err: unknown) {
            console.error(err);
            toast.error("Failed to update player status");
        }
    };

    // Start editing an existing player (fill the form with current values)
    const startEditPlayer = (player: PlayerDto) => {
        setFullName(player.fullName);
        setPhone(player.phone);
        setEmail(player.email);
        setActive(player.isActive);
        setEditingPlayerId(player.id);
        setIsFormOpen(true);
    };

    // Create a new player OR update an existing one
    const handleSavePlayer = async (
        e: FormEvent<HTMLFormElement>
    ) => {
        // If any HTML validation fails (required / pattern / type),
        // this handler will not be called at all.
        e.preventDefault();

        try {
            setIsSaving(true);

            const trimmedFullName = fullName.trim();
            const trimmedPhone = phone.trim();
            const trimmedEmail = email.trim();

            if (editingPlayerId) {
                // Update existing player
                await playersApi.updatePlayer({
                    id: editingPlayerId,
                    fullName: trimmedFullName,
                    phone: trimmedPhone,
                    email: trimmedEmail,
                });

                toast.success("Player updated");
            } else {
                // Create new player
                const player = await playersApi.createPlayer({
                    fullName: trimmedFullName,
                    phone: trimmedPhone,
                    email: trimmedEmail,
                });

                // Optionally activate immediately if checkbox is set
                if (active) {
                    try {
                        await playersApi.activatePlayer(player.id);
                    } catch (err: unknown) {
                        console.error(err);
                        toast.error(
                            "Player created, but activating failed"
                        );
                    }
                }

                toast.success("Player created");
            }

            resetForm();
            setIsFormOpen(false);
            await loadData();
        } catch (err: unknown) {
            console.error(err);
            toast.error(
                editingPlayerId
                    ? "Failed to update player"
                    : "Failed to create player"
            );
        } finally {
            setIsSaving(false);
        }
    };

    const handleOpenForm = () => {
        if (!isFormOpen) {
            resetForm();
        } else {
            setEditingPlayerId(null);
        }
        setIsFormOpen((prev) => !prev);
    };

    const handleStatusFilterChange = (value: StatusFilter) => {
        setStatusFilter(value);
    };

    const handleSort = (column: SortBy) => {
        if (sortBy === column) {
            setSortDirection(
                sortDirection === "asc" ? "desc" : "asc"
            );
        } else {
            setSortBy(column);
            setSortDirection("asc");
        }
    };

    const filterButtonClass = (value: StatusFilter) =>
        "rounded-full px-3 py-1 text-xs font-semibold border " +
        (statusFilter === value
            ? "bg-slate-900 text-white border-slate-900"
            : "bg-white text-slate-700 border-slate-300 hover:bg-slate-100");

    const renderSortArrow = (column: SortBy) => {
        if (sortBy !== column) return null;
        return (
            <span className="ml-1">
                {sortDirection === "asc" ? "↑" : "↓"}
            </span>
        );
    };

    return (
        <section className="bg-white rounded-2xl shadow-sm border border-slate-200 p-6">
            <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between mb-4">
                <div>
                    <h2 className="text-lg font-semibold text-slate-900">
                        Player Management
                    </h2>
                    <p className="text-xs text-slate-500">
                        Create, edit and manage all registered players
                    </p>
                </div>

                <div className="flex flex-wrap items-center gap-3">
                    {/* Status filter buttons */}
                    <div className="flex items-center gap-1">
                        <span className="text-xs text-slate-500 mr-1">
                            Status:
                        </span>
                        <button
                            type="button"
                            onClick={() =>
                                handleStatusFilterChange("all")
                            }
                            className={filterButtonClass("all")}
                        >
                            All
                        </button>
                        <button
                            type="button"
                            onClick={() =>
                                handleStatusFilterChange("active")
                            }
                            className={filterButtonClass("active")}
                        >
                            Active
                        </button>
                        <button
                            type="button"
                            onClick={() =>
                                handleStatusFilterChange("inactive")
                            }
                            className={filterButtonClass("inactive")}
                        >
                            Inactive
                        </button>
                    </div>

                    {/* Add player button */}
                    <button
                        type="button"
                        onClick={handleOpenForm}
                        className="inline-flex items-center rounded-full bg-slate-900 px-4 py-1.5 text-xs font-semibold text-white hover:bg-slate-800"
                    >
                        {isFormOpen ? "Close" : "Add Player"}
                    </button>
                </div>
            </div>

            {/* Create / Edit Player form */}
            {isFormOpen && (
                <form
                    // Wrap async handler so React gets a sync function and we explicitly ignore the Promise
                    onSubmit={(e: FormEvent<HTMLFormElement>) => {
                        void handleSavePlayer(e);
                    }}
                    className="mb-6 rounded-2xl bg-slate-50 p-4 grid gap-4 md:grid-cols-2 xl:grid-cols-4"
                >
                    <div className="md:col-span-2">
                        <label className="block text-xs font-semibold text-slate-700 mb-1">
                            Full Name
                        </label>
                        <input
                            // Text input with basic HTML validation
                            type="text"
                            required
                            minLength={3}
                            className="input input-bordered w-full text-sm bg-white"
                            placeholder="John Doe"
                            value={fullName}
                            onChange={(e) =>
                                setFullName(e.target.value)
                            }
                        />
                    </div>

                    <div>
                        <label className="block text-xs font-semibold text-slate-700 mb-1">
                            Phone Number
                        </label>
                        <input
                            // Use native validation for phone:
                            // only digits, spaces, '+' or '-'
                            type="tel"
                            required
                            pattern="^[0-9+\-\s]{4,30}$"
                            title="Phone number looks invalid. Use only digits, spaces, '+' or '-'."
                            inputMode="tel"
                            className="input input-bordered w-full text-sm bg-white"
                            placeholder="+45 12 34 56 78"
                            value={phone}
                            onChange={(e) =>
                                setPhone(e.target.value)
                            }
                        />
                    </div>

                    <div>
                        <label className="block text-xs font-semibold text-slate-700 mb-1">
                            Email
                        </label>
                        <input
                            // Browser will show a nice message if email is invalid
                            type="email"
                            required
                            className="input input-bordered w-full text-sm bg-white"
                            placeholder="john@example.com"
                            value={email}
                            onChange={(e) =>
                                setEmail(e.target.value)
                            }
                        />
                    </div>

                    <div className="flex items-center gap-3 md:col-span-2 xl:col-span-1">
                        <input
                            type="checkbox"
                            className="toggle toggle-sm"
                            checked={active}
                            onChange={(e) =>
                                setActive(e.target.checked)
                            }
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
                            {isSaving
                                ? "Saving..."
                                : editingPlayerId
                                    ? "Update"
                                    : "Save"}
                        </button>
                    </div>
                </form>
            )}

            {/* Players table */}
            {isLoading ? (
                <p className="text-sm text-slate-500">
                    Loading players...
                </p>
            ) : players.length === 0 ? (
                <p className="text-sm text-slate-500">
                    No players yet. Add your first player to get
                    started.
                </p>
            ) : (
                <div className="overflow-x-auto">
                    <table className="min-w-full text-sm">
                        <thead>
                        <tr className="border-b border-slate-200 text-left text-xs uppercase text-slate-500">
                            <th className="py-2 pr-4">
                                <button
                                    type="button"
                                    onClick={() =>
                                        handleSort("fullName")
                                    }
                                    className="inline-flex items-center gap-1"
                                >
                                    Name
                                    {renderSortArrow("fullName")}
                                </button>
                            </th>
                            <th className="py-2 pr-4">Phone</th>
                            <th className="py-2 pr-4">Email</th>
                            <th className="py-2 pr-4">Balance</th>
                            <th className="py-2 pr-4">Status</th>
                            <th className="py-2 pr-4">
                                <button
                                    type="button"
                                    onClick={() =>
                                        handleSort("createdAt")
                                    }
                                    className="inline-flex items-center gap-1"
                                >
                                    Joined
                                    {renderSortArrow("createdAt")}
                                </button>
                            </th>
                            <th className="py-2 pr-4 text-right">
                                Actions
                            </th>
                        </tr>
                        </thead>
                        <tbody>
                        {players.map((p) => {
                            const balance =
                                balances[p.id] ?? 0;
                            const created =
                                p.createdAt &&
                                new Date(
                                    p.createdAt
                                ).toLocaleDateString();

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
                                    <td className="py-3 pl-4 text-right space-x-2">
                                        {/* Edit button: fills the form with this player's data */}
                                        <button
                                            type="button"
                                            onClick={() =>
                                                startEditPlayer(
                                                    p
                                                )
                                            }
                                            className="inline-flex items-center rounded-full border border-slate-300 px-3 py-1 text-xs font-semibold text-slate-700 hover:bg-slate-100"
                                        >
                                            Edit
                                        </button>

                                        {/* Activate / Deactivate button */}
                                        <button
                                            type="button"
                                            // Wrap async toggleActive in a sync handler and explicitly ignore the Promise
                                            onClick={() => {
                                                void toggleActive(
                                                    p
                                                );
                                            }}
                                            className={
                                                "inline-flex items-center rounded-full border px-3 py-1 text-xs font-semibold " +
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
