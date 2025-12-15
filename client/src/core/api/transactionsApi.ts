// src/utilities/transactionsApi.ts
import {
    TransactionsClient,
    type TransactionResponseDto,
    type PlayerBalanceResponseDto,
    type CreateTransactionForCurrentUserRequestDto,
    type AdminCreateTransactionRequestDto,
} from "@core/api/generated/generated-client.ts";
import { baseUrl } from "@core/config/baseUrl.ts";
import { customFetch } from "@core/api/customFetch.ts";

/**
 * NSwag TransactionsClient instance using customFetch.
 * Methods keep the original names:
 * - createTransaction
 * - createTransactionForPlayer
 * - approveTransaction
 * - rejectTransaction
 * - getMyTransactions
 * - getPendingTransactions
 * - getMyBalance
 * - getPlayerBalance
 */
export const transactionsApi = new TransactionsClient(baseUrl, {
    fetch: customFetch,
});

// Helpful aliases for UI code
export type TransactionDto = TransactionResponseDto;
export type PlayerBalanceDto = PlayerBalanceResponseDto;
export type CreateMyTransactionDto = CreateTransactionForCurrentUserRequestDto;
export type AdminCreateTransactionDto = AdminCreateTransactionRequestDto;
