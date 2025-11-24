// src/hooks/useAdminPayments.ts
import { useEffect, useState } from "react";
import type { Player, Transaction } from "@core/generated-client";
import { transactionsApi } from "@utilities/transactionsApi";
import { playersApi } from "@utilities/playersApi";
import toast from "react-hot-toast";

type NewPaymentState = {
    playerId: string;
    amount: string;        // keep as string for the input
    mobilePayNumber: string;
};

// Hook with all admin payments logic (data + form)
export function useAdminPayments() {
    const [pending, setPending] = useState<Transaction[]>([]);
    const [players, setPlayers] = useState<Player[]>([]);
    const [isLoading, setIsLoading] = useState(true);

    const [isAdding, setIsAdding] = useState(false);
    const [isSaving, setIsSaving] = useState(false);
    const [form, setForm] = useState<NewPaymentState>({
        playerId: "",
        amount: "",
        mobilePayNumber: "",
    });

    // Load pending transactions
    const loadPending = async () => {
        try {
            setIsLoading(true);
            const list = await transactionsApi.getPendingTransactions();
            setPending(Array.isArray(list) ? list : []);
        } catch (err) {
            console.error(err);
            toast.error("Failed to load pending transactions");
        } finally {
            setIsLoading(false);
        }
    };

    // Load active players for the dropdown
    const loadPlayers = async () => {
        try {
            const list = await playersApi.getPlayers(true);
            setPlayers(Array.isArray(list) ? list : []);
        } catch (err) {
            console.error(err);
            toast.error("Failed to load players for payments");
        }
    };

    useEffect(() => {
        void loadPending();
        void loadPlayers();
    }, []);

    // Approve a pending transaction
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

    // Reject a pending transaction
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

    // Validate and create new payment
    const saveNewPayment = async () => {
        if (!form.playerId) {
            toast.error("Please select a player");
            return;
        }

        const amountNumber = Number(form.amount.replace(",", "."));
        if (!Number.isFinite(amountNumber) || amountNumber <= 0) {
            toast.error("Amount must be a positive number");
            return;
        }

        if (!form.mobilePayNumber.trim()) {
            toast.error("MobilePay number is required");
            return;
        }

        try {
            setIsSaving(true);

            await transactionsApi.createTransactionForPlayer({
                playerId: form.playerId,
                mobilePayNumber: form.mobilePayNumber.trim(),
                amount: amountNumber,
            });

            toast.success("Payment created and waiting for approval");

            // Reset form and close the editor
            setForm({
                playerId: "",
                amount: "",
                mobilePayNumber: "",
            });
            setIsAdding(false);
            await loadPending();
        } catch (err) {
            console.error(err);
            toast.error("Failed to create payment");
        } finally {
            setIsSaving(false);
        }
    };

    return {
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
    };
}
