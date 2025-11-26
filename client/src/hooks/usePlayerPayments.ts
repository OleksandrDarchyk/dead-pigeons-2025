// client/src/hooks/usePlayerPayments.ts
import { useEffect, useState } from "react";
import { transactionsApi } from "../utilities/transactionsApi";
import type { Transaction } from "../core/generated-client";
import toast from "react-hot-toast";

type NewPaymentForm = {
    amount: string;
    mobilePayNumber: string;
};

export function usePlayerPayments() {
    const [payments, setPayments] = useState<Transaction[]>([]);
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

            await transactionsApi.createTransaction({
                playerId: "", // player is resolved from the logged-in user on the server
                amount: amountNumber,
                mobilePayNumber: form.mobilePayNumber.trim(),
            });

            toast.success("Payment submitted for approval");

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
