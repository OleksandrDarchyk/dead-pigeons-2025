// client/src/hooks/usePlayerPayments.ts
import { useEffect, useState } from "react";
import { transactionsApi } from "../utilities/transactionsApi";
import type { TransactionResponseDto } from "../core/generated-client";
import toast from "react-hot-toast";

type NewPaymentForm = {
    amount: string;
    mobilePayNumber: string;
};

export function usePlayerPayments() {
    const [payments, setPayments] = useState<TransactionResponseDto[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [isSaving, setIsSaving] = useState(false);
    const [form, setForm] = useState<NewPaymentForm>({
        amount: "",
        mobilePayNumber: "",
    });

    // Load payments when the player page is opened
    const loadPayments = async () => {
        try {
            setIsLoading(true);

            // getMyTransactions() already returns TransactionResponseDto[]
            const list = await transactionsApi.getMyTransactions();
            setPayments(Array.isArray(list) ? list : []);
        } catch (err) {
            console.error(err);
            toast.error("Failed to load your payments");
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => {
        void loadPayments();
    }, []);

    const submitPayment = async () => {
        // 1) Normalize amount: replace comma with dot and trim spaces
        const normalizedAmount = form.amount.replace(",", ".").trim();
        const amountNumber = Number(normalizedAmount);

        if (!Number.isFinite(amountNumber) || amountNumber <= 0) {
            toast.error("Amount must be a positive number");
            return;
        }

        // 2) Basic MobilePay validation
        const mobilePay = form.mobilePayNumber.trim();
        if (!mobilePay) {
            toast.error("MobilePay number is required");
            return;
        }

        // Allow digits, spaces, + and -, length between 4 and 30
        const mobilePayPattern = /^[0-9+\-\s]{4,30}$/;
        if (!mobilePayPattern.test(mobilePay)) {
            toast.error("MobilePay number looks invalid");
            return;
        }

        try {
            setIsSaving(true);

            // Player does NOT send playerId; server resolves player from JWT
            await transactionsApi.createTransaction({
                amount: amountNumber,
                mobilePayNumber: mobilePay,
            });

            toast.success("Payment submitted for approval");

            // Reset form and reload list
            setForm({
                amount: "",
                mobilePayNumber: "",
            });

            await loadPayments();
        } catch (err) {
            console.error(err);
            toast.error("Failed to submit payment");
        } finally {
            setIsSaving(false);
        }
    };

    return {
        payments,
        isLoading,
        isSaving,
        form,
        setForm,
        submitPayment,
    };
}
