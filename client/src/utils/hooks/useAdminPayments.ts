import { useEffect, useState } from "react";
import type { TransactionResponseDto } from "@core/api/generated/generated-client";
import { transactionsApi } from "@core/api/transactionsApi";
import { playersApi, type PlayerDto } from "@core/api/playersApi";
import toast from "react-hot-toast";

type NewPaymentState = {
    playerId: string;
    amount: string;
    mobilePayNumber: string;
};

const mobilePayCleanPattern = /^[0-9+\-\s]{4,30}$/;

export function useAdminPayments() {
    const [pending, setPending] = useState<TransactionResponseDto[]>([]);
    const [players, setPlayers] = useState<PlayerDto[]>([]);
    const [isLoading, setIsLoading] = useState(true);

    const [isAdding, setIsAdding] = useState(false);
    const [isSaving, setIsSaving] = useState(false);

    const [form, setForm] = useState<NewPaymentState>({
        playerId: "",
        amount: "",
        mobilePayNumber: "",
    });

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

    const saveNewPayment = async () => {
        if (!form.playerId) {
            toast.error("Please select a player");
            return;
        }

        const normalizedAmount = form.amount.trim().replace(",", ".");
        const amountNumber = Number(normalizedAmount);

        if (!Number.isFinite(amountNumber) || amountNumber <= 0) {
            toast.error("Amount must be a positive number");
            return;
        }

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

            await transactionsApi.createTransactionForPlayer({
                playerId: form.playerId,
                mobilePayNumber: mobilePay,
                amount: amountNumber,
            });

            toast.success("Payment created and waiting for approval");

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
