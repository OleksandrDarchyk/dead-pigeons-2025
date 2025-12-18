import {
    TransactionsClient,
    type TransactionResponseDto,
    type PlayerBalanceResponseDto,
    type CreateTransactionForCurrentUserRequestDto,
    type AdminCreateTransactionRequestDto,
} from "@core/api/generated/generated-client.ts";
import { baseUrl } from "@core/config/baseUrl.ts";
import { customFetch } from "@core/api/customFetch.ts";

export const transactionsApi = new TransactionsClient(baseUrl, {
    fetch: customFetch,
});

export type TransactionDto = TransactionResponseDto;
export type PlayerBalanceDto = PlayerBalanceResponseDto;
export type CreateMyTransactionDto = CreateTransactionForCurrentUserRequestDto;
export type AdminCreateTransactionDto = AdminCreateTransactionRequestDto;
