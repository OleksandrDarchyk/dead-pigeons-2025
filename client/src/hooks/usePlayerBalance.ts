// client/src/hooks/usePlayerBalance.ts
import { useEffect, useState } from "react";
import { transactionsApi } from "../utilities/transactionsApi";
import type { PlayerBalanceResponseDto } from "../core/generated-client";
import toast from "react-hot-toast";

export function usePlayerBalance(enabled: boolean = true) {
    const [balance, setBalance] = useState<number | null>(null);
    const [isLoading, setIsLoading] = useState(enabled);

    useEffect(() => {
        if (!enabled) {
            setIsLoading(false);
            return;
        }

        const loadBalance = async () => {
            try {
                setIsLoading(true);

                const balanceDto: PlayerBalanceResponseDto =
                    await transactionsApi.getMyBalance();

                setBalance(balanceDto.balance);
            } catch (err) {
                console.error(err);
                toast.error("Failed to load your balance");
            } finally {
                setIsLoading(false);
            }
        };

        void loadBalance();
    }, [enabled]);

    return {
        balance,
        isLoading,
    };
}
