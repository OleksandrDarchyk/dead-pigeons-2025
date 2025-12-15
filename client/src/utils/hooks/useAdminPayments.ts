// src/hooks/useAdminPayments.ts
import { useEffect, useState } from "react";
import type { TransactionResponseDto } from "@core/api/generated/generated-client";
import { transactionsApi } from "@core/api/transactionsApi";
import { playersApi, type PlayerDto } from "@core/api/playersApi";
import toast from "react-hot-toast";



type NewPaymentState = {
    // id of selected player from the dropdown
    playerId: string;
    // keep as string for controlled input and to allow comma or spaces
    amount: string;
    // raw MobilePay transaction number typed by the admin
    mobilePayNumber: string;
};

// - allow digits, spaces, + and -
// - length between 4 and 30 characters
const mobilePayCleanPattern = /^[0-9+\-\s]{4,30}$/;

/**
 * Admin payments hook:
 * - loads pending transactions (DTOs)
 * - loads active players (DTOs)
 * - exposes approve/reject actions
 * - exposes form state for creating new payments
 *
 * Security notes:
 * - We only work with DTOs (TransactionResponseDto / PlayerDto), not EF entities.
 * - All real validation (player existence, amount, balance) is still done on the server.
 * - customFetch attaches JWT and handles ProblemDetails error responses globally.
 */
export function useAdminPayments() {
    // Pending MobilePay transactions waiting for approval
    const [pending, setPending] = useState<TransactionResponseDto[]>([]);
    // Active players (DTOs from the API)
    const [players, setPlayers] = useState<PlayerDto[]>([]);
    const [isLoading, setIsLoading] = useState(true);

    // UI state for "add payment" panel
    const [isAdding, setIsAdding] = useState(false);
    const [isSaving, setIsSaving] = useState(false);

    // Local form state for creating a new payment
    const [form, setForm] = useState<NewPaymentState>({
        playerId: "",
        amount: "",
        mobilePayNumber: "",
    });

    // Load pending transactions (DTO list)
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

    // Load active players for the dropdown (only active = true)
    const loadPlayers = async () => {
        try {
            const list = await playersApi.getPlayers(true);
            setPlayers(Array.isArray(list) ? list : []);
        } catch (err) {
            console.error(err);
            toast.error("Failed to load players for payments");
        }
    };

    // Initial load: pending transactions + players list
    useEffect(() => {
        void loadPending();
        void loadPlayers();
    }, []);

    // Approve a pending transaction
    const approve = async (tx: TransactionResponseDto) => {
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
    const reject = async (tx: TransactionResponseDto) => {
        try {
            await transactionsApi.rejectTransaction(tx.id);
            toast.success("Transaction rejected");
            await loadPending();
        } catch (err) {
            console.error(err);
            toast.error("Failed to reject transaction");
        }
    };

    // Validate and create new payment (admin creates MobilePay tx for a player)
    const saveNewPayment = async () => {
        // 1) validate player
        if (!form.playerId) {
            toast.error("Please select a player");
            return;
        }

        // 2) validate amount (normalize comma → dot)
        const normalizedAmount = form.amount.trim().replace(",", ".");
        const amountNumber = Number(normalizedAmount);

        if (!Number.isFinite(amountNumber) || amountNumber <= 0) {
            toast.error("Amount must be a positive number");
            return;
        }

        // 3) validate MobilePay number (basic shape check)
        const mobilePay = form.mobilePayNumber.trim();

        if (!mobilePay) {
            toast.error("MobilePay number is required");
            return;
        }

        if (!mobilePayCleanPattern.test(mobilePay)) {
            toast.error("MobilePay number looks invalid");
            return;
        }

        try {
            setIsSaving(true);

            // Admin explicitly chooses playerId; server still checks that player exists
            await transactionsApi.createTransactionForPlayer({
                playerId: form.playerId,
                mobilePayNumber: mobilePay,
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
