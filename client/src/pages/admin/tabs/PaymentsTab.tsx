// src/components/admin/tabs/PaymentsTab.tsx
import { useAdminPayments } from "../../../hooks/useAdminPayments";

export default function PaymentsTab() {
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

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        await saveNewPayment();
    };

    return (
        <section className="bg-white rounded-2xl shadow-sm border border-slate-200 p-6">
            <div className="flex items-center justify-between mb-4">
                <div>
                    <h2 className="text-lg font-semibold text-slate-900">
                        Payment Approval Queue
                    </h2>
                    <p className="text-xs text-slate-500">
                        {pending.length} pending transaction
                        {pending.length === 1 ? "" : "s"}
                    </p>
                </div>

                <button
                    type="button"
                    onClick={() => setIsAdding((x) => !x)}
                    className="inline-flex items-center rounded-full bg-slate-900 px-4 py-1.5 text-xs font-semibold text-white hover:bg-slate-800"
                >
                    {isAdding ? "Close" : "Add Payment"}
                </button>
            </div>

            {isAdding && (
                <form
                    onSubmit={handleSubmit}
                    className="mb-6 grid gap-3 md:grid-cols-[2fr,1fr,1fr,auto]"
                >
                    <select
                        className="select select-bordered w-full text-sm bg-slate-50"
                        value={form.playerId}
                        onChange={(e) =>
                            setForm((prev) => ({
                                ...prev,
                                playerId: e.target.value,
                            }))
                        }
                    >
                        <option value="">Select player…</option>
                        {players.map((p) => (
                            <option key={p.id} value={p.id}>
                                {p.fullname} ({p.phone})
                            </option>
                        ))}
                    </select>

                    <input
                        type="text"
                        className="input input-bordered w-full text-sm bg-slate-50"
                        placeholder="Amount (DKK)"
                        value={form.amount}
                        onChange={(e) =>
                            setForm((prev) => ({
                                ...prev,
                                amount: e.target.value,
                            }))
                        }
                    />

                    <input
                        type="text"
                        className="input input-bordered w-full text-sm bg-slate-50"
                        placeholder="MobilePay number"
                        value={form.mobilePayNumber}
                        onChange={(e) =>
                            setForm((prev) => ({
                                ...prev,
                                mobilePayNumber: e.target.value,
                            }))
                        }
                    />

                    <button
                        type="submit"
                        disabled={isSaving}
                        className="rounded-full bg-red-600 px-4 py-2 text-xs font-semibold text-white hover:bg-red-700 disabled:opacity-60"
                    >
                        {isSaving ? "Saving…" : "Save"}
                    </button>
                </form>
            )}

            {isLoading ? (
                <p className="text-sm text-slate-500">Loading transactions...</p>
            ) : pending.length === 0 ? (
                <p className="text-sm text-slate-500">
                    No pending transactions.
                </p>
            ) : (
                <div className="overflow-x-auto">
                    <table className="min-w-full text-sm">
                        <thead>
                        <tr className="border-b border-slate-200 text-left text-xs uppercase text-slate-500">
                            <th className="py-2 pr-4">Player</th>
                            <th className="py-2 pr-4">Amount</th>
                            <th className="py-2 pr-4">MobilePay #</th>
                            <th className="py-2 pr-4">Date</th>
                            <th className="py-2 pr-4 text-right">
                                Actions
                            </th>
                        </tr>
                        </thead>
                        <tbody>
                        {pending.map((tx) => {
                            const playerName =
                                tx.player?.fullname ?? "Unknown player";
                            const date =
                                tx.createdat &&
                                new Date(
                                    tx.createdat
                                ).toLocaleDateString();

                            return (
                                <tr
                                    key={tx.id}
                                    className="border-b border-slate-100 last:border-0"
                                >
                                    <td className="py-3 pr-4 text-slate-900">
                                        {playerName}
                                    </td>
                                    <td className="py-3 pr-4 text-slate-900">
                                        {tx.amount.toFixed(2)} DKK
                                    </td>
                                    <td className="py-3 pr-4 text-slate-700">
                                        {tx.mobilepaynumber}
                                    </td>
                                    <td className="py-3 pr-4 text-slate-500">
                                        {date ?? "–"}
                                    </td>
                                    <td className="py-3 pl-4 text-right space-x-2">
                                        <button
                                            type="button"
                                            onClick={() => approve(tx)}
                                            className="inline-flex items-center rounded-full bg-green-600 px-4 py-1.5 text-xs font-semibold text-white hover:bg-green-700"
                                        >
                                            Approve
                                        </button>
                                        <button
                                            type="button"
                                            onClick={() => reject(tx)}
                                            className="inline-flex items-center rounded-full bg-red-600 px-4 py-1.5 text-xs font-semibold text-white hover:bg-red-700"
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
        </section>
    );
}
