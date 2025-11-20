// src/utilities/transactionsApi.ts
import { TransactionsClient } from "@core/generated-client";
import { baseUrl } from "@core/baseUrl";
import { customFetch } from "@utilities/customFetch";

export const transactionsApi = new TransactionsClient(baseUrl, customFetch);
