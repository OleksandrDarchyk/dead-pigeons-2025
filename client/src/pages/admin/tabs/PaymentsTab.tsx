// src/components/admin/PaymentsTab.tsx
import { useEffect, useState } from "react";
import type { Transaction } from "../../../core/generated-client";
import { transactionsApi } from "../../../utilities/transactionsApi";
import toast from "react-hot-toast";



export default function PaymentsTab() {
    const [pending, setPending] = useState<Transaction[]>([]);
    const [isLoading, setIsLoading] = useState(true);

    const loadPending = async () => {
        try {
            setIsLoading(true);

            const list = await transactionsApi.getPendingTransactions();

            console.log("getPendingTransactions result:", list);

            if (Array.isArray(list)) {
                setPending(list);
            } else {
                setPending([]);
            }
        } catch (err) {
            console.error(err);
            toast.error("Failed to load pending transactions");
        } finally {
            setIsLoading(false);
        }
    };


    useEffect(() => {
        void loadPending();
    }, []);

    const approve = async (tx: Transaction) => {
        try {
            await transactionsApi.approveTransaction(tx.id);
            toast.success("Transaction approved");
            await loadPending();
        } catch (err) {
            console.error(err);
            toast.error("Failed to approve transaction");
        }
    };

    const reject = async (tx: Transaction) => {
        try {
            await transactionsApi.rejectTransaction(tx.id);
            toast.success("Transaction rejected");
            await loadPending();
        } catch (err) {
            console.error(err);
            toast.error("Failed to reject transaction");
        }
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
            </div>

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
                                new Date(tx.createdat).toLocaleDateString();

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
