// client/src/pages/player/PlayerPaymentsSection.tsx
import { usePlayerPayments } from "../../hooks/usePlayerPayments";
import type { FormEvent } from "react";

export default function PlayerPaymentsSection() {
    // Custom hook for player payments:
    // keeps the list, the form state and the submit logic in one place
    const {
        payments,
        isLoading,
        isSaving,
        form,
        setForm,
        submitPayment,
    } = usePlayerPayments();

    // Form submit handler:
    // - prevent default browser navigation
    // - call async submitPayment() from the hook
    // We keep this function NON-async and use `void` so ESLint
    // does not complain about a Promise-returning handler.
    const handleSubmit = (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        void submitPayment();
    };

    return (
        <section className="bg-white rounded-2xl shadow-sm border border-slate-200 p-6">
            <div className="flex items-center justify-between mb-4">
                <div>
                    <h2 className="text-lg font-semibold text-slate-900">
                        My Payments
                    </h2>
                    <p className="text-xs text-slate-500">
                        Submit new payments and see their status.
                    </p>
                </div>
            </div>

            {/* New payment form */}
            <form
                onSubmit={handleSubmit}
                className="mb-6 grid gap-3 md:grid-cols-[1fr,1fr,auto]"
            >
                <input
                    // Amount is kept as string in form state and validated in the hook
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
                    // MobilePay number as free text (validated on the server side)
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
                    {isSaving ? "Sending..." : "Send payment"}
                </button>
            </form>

            {/* Payments list */}
            {isLoading ? (
                <p className="text-sm text-slate-500">
                    Loading payments...
                </p>
            ) : payments.length === 0 ? (
                <p className="text-sm text-slate-500">
                    You have no payments yet.
                </p>
            ) : (
                <div className="overflow-x-auto">
                    <table className="min-w-full text-sm">
                        <thead>
                        <tr className="border-b border-slate-200 text-left text-xs uppercase text-slate-500">
                            <th className="py-2 pr-4">Amount</th>
                            <th className="py-2 pr-4">
                                MobilePay #
                            </th>
                            <th className="py-2 pr-4">Status</th>
                            <th className="py-2 pr-4">Date</th>
                        </tr>
                        </thead>
                        <tbody>
                        {payments.map((tx) => {
                            // createdAt comes from the API; format as local date
                            const date =
                                tx.createdAt &&
                                new Date(
                                    tx.createdAt,
                                ).toLocaleDateString();

                            const status = tx.status;
                            const statusClass =
                                status === "Approved"
                                    ? "bg-green-100 text-green-700"
                                    : status === "Rejected"
                                        ? "bg-red-100 text-red-700"
                                        : "bg-amber-100 text-amber-700";

                            return (
                                <tr
                                    key={tx.id}
                                    className="border-b border-slate-100 last:border-0"
                                >
                                    <td className="py-3 pr-4 text-slate-900">
                                        {tx.amount.toFixed(2)} DKK
                                    </td>
                                    <td className="py-3 pr-4 text-slate-700">
                                        {tx.mobilePayNumber}
                                    </td>
                                    <td className="py-3 pr-4">
                                            <span
                                                className={
                                                    "inline-flex items-center rounded-full px-3 py-1 text-xs font-semibold " +
                                                    statusClass
                                                }
                                            >
                                                {status}
                                            </span>
                                    </td>
                                    <td className="py-3 pr-4 text-slate-500">
                                        {date ?? "–"}
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
