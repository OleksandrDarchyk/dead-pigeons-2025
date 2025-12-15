// client/src/pages/admin/tabs/PaymentsTab.tsx

import { useState } from "react";
import { useAdminPayments } from "@hooks/useAdminPayments";
import type { TransactionResponseDto } from "@core/api/generated/generated-client";
import toast from "react-hot-toast";
import {transactionsApi} from "@core/api/transactionsApi.ts";


type HistoryStatusFilter = "All" | "Pending" | "Approved" | "Rejected";

/**
 * Admin Payments tab:
 * - shows all pending MobilePay transactions
 * - lets admin approve / reject them
 * - lets admin create a new payment for a selected player
 * - shows transaction history for a chosen player
 */
export default function PaymentsTab() {
    // Pending payments + add-payment form state come from the hook
    const {
        pending,
        players,
        isLoading,
        isAdding,
        isSaving,
        form,
        setForm,
        setIsAdding,
        approve,
        reject,
        saveNewPayment,
    } = useAdminPayments();

    // History filter and data (admin side)
    const [historyPlayerId, setHistoryPlayerId] = useState<string>("");
    const [historyStatus, setHistoryStatus] =
        useState<HistoryStatusFilter>("All");
    const [history, setHistory] = useState<TransactionResponseDto[]>([]);
    const [isHistoryLoading, setIsHistoryLoading] = useState(false);

    // Helper: show a readable player name from playerId
    const getPlayerName = (playerId: string): string => {
        const player = players.find((p) => p.id === playerId);
        return player?.fullName ?? "Unknown player";
    };

    // Helper: update a single field in the "new payment" form
    const handleChange = (
        field: "playerId" | "amount" | "mobilePayNumber",
        value: string,
    ) => {
        setForm((prev) => ({
            ...prev,
            [field]: value,
        }));
    };

    // Helper: reset form to default empty values
    const resetForm = () => {
        setForm({
            playerId: "",
            amount: "",
            mobilePayNumber: "",
        });
    };

    // Load transaction history for selected player + status
    const loadHistory = async () => {
        if (!historyPlayerId) {
            toast.error("Please select a player for history");
            return;
        }

        try {
            setIsHistoryLoading(true);

            // "All" means no status filter on the API call
            const statusParam =
                historyStatus === "All" ? undefined : historyStatus;

            const list = await transactionsApi.getTransactionsHistory(
                historyPlayerId,
                statusParam,
            );

            setHistory(Array.isArray(list) ? list : []);
        } catch (err) {
            console.error(err);
            toast.error("Failed to load transaction history");
        } finally {
            setIsHistoryLoading(false);
        }
    };

    return (
        <section className="bg-white rounded-2xl shadow-sm border border-slate-200 p-6">
            {/* HEADER */}
            <div className="mb-4 flex items-center justify-between">
                <div>
                    <h2 className="text-lg font-semibold text-slate-900">
                        Payments &amp; Balances
                    </h2>
                    <p className="text-xs text-slate-500">
                        Review pending MobilePay transactions and create new
                        payments for players.
                    </p>
                </div>

                <button
                    type="button"
                    onClick={() => {
                        // When opening the form → start with a clean state
                        if (!isAdding) {
                            resetForm();
                            setIsAdding(true);
                        } else {
                            // When closing → just hide the form
                            setIsAdding(false);
                        }
                    }}
                    className="inline-flex items-center rounded-full bg-slate-900 px-4 py-1.5 text-xs font-semibold text-white hover:bg-slate-800"
                >
                    {isAdding ? "Close" : "Add Payment"}
                </button>
            </div>

            {/* NEW PAYMENT FORM */}
            {isAdding && (
                <form
                    onSubmit={(e) => {
                        e.preventDefault();
                        void saveNewPayment();
                    }}
                    className="mb-6 rounded-2xl bg-slate-50 p-4 grid gap-4 md:grid-cols-2 xl:grid-cols-4"
                >
                    {/* Player select */}
                    <div>
                        <label className="block text-xs font-semibold text-slate-700 mb-1">
                            Player
                        </label>
                        <select
                            className="select select-bordered w-full text-sm bg-white"
                            value={form.playerId}
                            onChange={(e) =>
                                handleChange("playerId", e.target.value)
                            }
                        >
                            <option value="">Select player</option>
                            {players.map((p) => (
                                <option key={p.id} value={p.id}>
                                    {p.fullName}
                                </option>
                            ))}
                        </select>
                    </div>

                    {/* Amount */}
                    <div>
                        <label className="block text-xs font-semibold text-slate-700 mb-1">
                            Amount (DKK)
                        </label>
                        <input
                            type="text"
                            className="input input-bordered w-full text-sm bg-white"
                            placeholder="e.g. 100"
                            value={form.amount}
                            onChange={(e) =>
                                handleChange("amount", e.target.value)
                            }
                        />
                        <p className="mt-1 text-[11px] text-slate-500">
                            Use dot or comma, e.g. <code>100</code> or{" "}
                            <code>100,50</code>.
                        </p>
                    </div>

                    {/* MobilePay number */}
                    <div>
                        <label className="block text-xs font-semibold text-slate-700 mb-1">
                            MobilePay Transaction Number
                        </label>
                        <input
                            type="text"
                            className="input input-bordered w-full text-sm bg-white"
                            placeholder="MP-000123"
                            value={form.mobilePayNumber}
                            onChange={(e) =>
                                handleChange(
                                    "mobilePayNumber",
                                    e.target.value,
                                )
                            }
                        />
                    </div>

                    {/* Buttons */}
                    <div className="flex items-end justify-end gap-2 md:col-span-2 xl:col-span-1">
                        <button
                            type="button"
                            onClick={() => {
                                // Cancel → clear form + hide panel
                                resetForm();
                                setIsAdding(false);
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
                            {isSaving ? "Saving..." : "Save Payment"}
                        </button>
                    </div>
                </form>
            )}

            {/* PENDING TRANSACTIONS LIST */}
            <div>
                <h3 className="mb-2 text-sm font-semibold text-slate-900">
                    Pending Transactions
                </h3>
                <p className="mb-4 text-xs text-slate-500">
                    These payments are waiting for approval or rejection.
                </p>

                {isLoading ? (
                    <p className="text-sm text-slate-500">
                        Loading transactions...
                    </p>
                ) : pending.length === 0 ? (
                    <p className="text-sm text-slate-500">
                        No pending transactions at the moment.
                    </p>
                ) : (
                    <div className="overflow-x-auto">
                        <table className="min-w-full text-sm">
                            <thead>
                            <tr className="border-b border-slate-200 text-left text-xs uppercase text-slate-500">
                                <th className="py-2 pr-4">Player</th>
                                <th className="py-2 pr-4">
                                    MobilePay No.
                                </th>
                                <th className="py-2 pr-4">Amount</th>
                                <th className="py-2 pr-4">Created</th>
                                <th className="py-2 pr-4 text-right">
                                    Actions
                                </th>
                            </tr>
                            </thead>
                            <tbody>
                            {pending.map((tx) => {
                                const playerName = getPlayerName(
                                    tx.playerId,
                                );
                                const created = new Date(
                                    tx.createdAt,
                                ).toLocaleString();

                                return (
                                    <tr
                                        key={tx.id}
                                        className="border-b border-slate-100 last:border-0"
                                    >
                                        <td className="py-3 pr-4 font-medium text-slate-900">
                                            {playerName}
                                        </td>
                                        <td className="py-3 pr-4 text-slate-700">
                                            {tx.mobilePayNumber}
                                        </td>
                                        <td className="py-3 pr-4 text-slate-900">
                                            {tx.amount.toFixed(2)} DKK
                                        </td>
                                        <td className="py-3 pr-4 text-slate-500">
                                            {created}
                                        </td>
                                        <td className="py-3 pl-4 text-right">
                                            <button
                                                type="button"
                                                onClick={() =>
                                                    void approve(tx)
                                                }
                                                className="mr-2 inline-flex items-center rounded-full border border-green-300 px-3 py-1 text-xs font-semibold text-green-700 hover:bg-green-50"
                                            >
                                                Approve
                                            </button>
                                            <button
                                                type="button"
                                                onClick={() =>
                                                    void reject(tx)
                                                }
                                                className="inline-flex items-center rounded-full border border-red-300 px-3 py-1 text-xs font-semibold text-red-700 hover:bg-red-50"
                                            >
                                                Reject
                                            </button>
                                        </td>
                                    </tr>
                                );
                            })}
                            </tbody>
                        </table>
                    </div>
                )}
            </div>

            {/* TRANSACTION HISTORY */}
            <div className="mt-8 border-t border-slate-200 pt-6">
                <div className="mb-4 flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
                    <div>
                        <h3 className="text-sm font-semibold text-slate-900">
                            Transaction history
                        </h3>
                        <p className="text-xs text-slate-500">
                            View previously approved or rejected payments for a
                            player.
                        </p>
                    </div>

                    <div className="flex flex-col gap-2 md:flex-row md:items-end">
                        {/* Player filter */}
                        <div>
                            <label className="block text-xs font-semibold text-slate-700 mb-1">
                                Player
                            </label>
                            <select
                                className="select select-bordered w-full text-sm bg-white min-w-[180px]"
                                value={historyPlayerId}
                                onChange={(e) =>
                                    setHistoryPlayerId(e.target.value)
                                }
                            >
                                <option value="">Select player</option>
                                {players.map((p) => (
                                    <option key={p.id} value={p.id}>
                                        {p.fullName}
                                    </option>
                                ))}
                            </select>
                        </div>

                        {/* Status filter */}
                        <div>
                            <label className="block text-xs font-semibold text-slate-700 mb-1">
                                Status
                            </label>
                            <select
                                className="select select-bordered w-full text-sm bg-white"
                                value={historyStatus}
                                onChange={(e) =>
                                    setHistoryStatus(
                                        e.target.value as HistoryStatusFilter,
                                    )
                                }
                            >
                                <option value="All">All</option>
                                <option value="Pending">Pending</option>
                                <option value="Approved">Approved</option>
                                <option value="Rejected">Rejected</option>
                            </select>
                        </div>

                        {/* Load button */}
                        <button
                            type="button"
                            onClick={() => void loadHistory()}
                            disabled={isHistoryLoading || !historyPlayerId}
                            className="mt-2 md:mt-0 rounded-full bg-slate-900 px-4 py-1.5 text-xs font-semibold text-white hover:bg-slate-800 disabled:opacity-60"
                        >
                            {isHistoryLoading ? "Loading..." : "Load history"}
                        </button>
                    </div>
                </div>

                {/* History table */}
                {history.length === 0 ? (
                    <p className="text-sm text-slate-500">
                        No transactions to show yet. Select a player and load
                        history.
                    </p>
                ) : (
                    <div className="overflow-x-auto">
                        <table className="min-w-full text-sm">
                            <thead>
                            <tr className="border-b border-slate-200 text-left text-xs uppercase text-slate-500">
                                <th className="py-2 pr-4">Status</th>
                                <th className="py-2 pr-4">
                                    MobilePay No.
                                </th>
                                <th className="py-2 pr-4">Amount</th>
                                <th className="py-2 pr-4">Created</th>
                                <th className="py-2 pr-4">
                                    Approved / Rejected
                                </th>
                            </tr>
                            </thead>
                            <tbody>
                            {history.map((tx) => {
                                const created = new Date(
                                    tx.createdAt,
                                ).toLocaleString();
                                const approved = tx.approvedAt
                                    ? new Date(
                                        tx.approvedAt,
                                    ).toLocaleString()
                                    : "-";

                                return (
                                    <tr
                                        key={tx.id}
                                        className="border-b border-slate-100 last:border-0"
                                    >
                                        <td className="py-3 pr-4 font-medium text-slate-900">
                                            {tx.status}
                                        </td>
                                        <td className="py-3 pr-4 text-slate-700">
                                            {tx.mobilePayNumber}
                                        </td>
                                        <td className="py-3 pr-4 text-slate-900">
                                            {tx.amount.toFixed(2)} DKK
                                        </td>
                                        <td className="py-3 pr-4 text-slate-500">
                                            {created}
                                        </td>
                                        <td className="py-3 pr-4 text-slate-500">
                                            {approved}
                                        </td>
                                    </tr>
                                );
                            })}
                            </tbody>
                        </table>
                    </div>
                )}
            </div>
        </section>
    );
}
