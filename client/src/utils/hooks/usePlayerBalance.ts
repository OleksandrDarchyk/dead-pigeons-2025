import { useCallback, useEffect, useState } from "react";
import { transactionsApi } from "@core/api/transactionsApi";
import type { PlayerBalanceResponseDto } from "@core/api/generated/generated-client";
import toast from "react-hot-toast";


export function usePlayerBalance(enabled: boolean = true) {
    const [balance, setBalance] = useState<number | null>(null);
    const [isLoading, setIsLoading] = useState<boolean>(enabled);

    const loadBalance = useCallback(async () => {
        if (!enabled) return;

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
    }, [enabled]);

    useEffect(() => {
        if (!enabled) {
            setIsLoading(false);
            return;
        }

        void loadBalance();
    }, [enabled, loadBalance]);

    return {
        balance,
        isLoading,
        reloadBalance: loadBalance,
    };
}
