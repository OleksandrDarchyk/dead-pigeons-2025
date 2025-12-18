import { useEffect, useState } from "react";
import { transactionsApi } from "@core/api/transactionsApi";
import type { TransactionResponseDto } from "@core/api/generated/generated-client";
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
        const normalizedAmount = form.amount.replace(",", ".").trim();
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

        try {
            setIsSaving(true);

            await transactionsApi.createTransaction({
                amount: amountNumber,
                mobilePayNumber: mobilePay,
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
