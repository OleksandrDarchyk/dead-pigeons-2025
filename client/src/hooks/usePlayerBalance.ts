// client/src/hooks/usePlayerBalance.ts
import { useEffect, useState } from "react";
import { transactionsApi } from "../utilities/transactionsApi";
import type { PlayerBalanceResponseDto } from "../core/generated-client";
import toast from "react-hot-toast";

// Hook for reading and reloading the current player's balance
export function usePlayerBalance(enabled: boolean = true) {
    const [balance, setBalance] = useState<number | null>(null);
    const [isLoading, setIsLoading] = useState<boolean>(enabled);

    // Single function that loads the balance from the API
    const loadBalance = async () => {
        // Do nothing if the hook is disabled
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
    };

    // Auto-load balance when "enabled" becomes true (for example when user is a player)
    useEffect(() => {
        if (!enabled) {
            setIsLoading(false);
            return;
        }

        void loadBalance();
    }, [enabled]);

    return {
        balance,
        isLoading,
        // Expose this so pages can force-refresh balance after actions (like buying a board)
        reloadBalance: loadBalance,
    };
}
