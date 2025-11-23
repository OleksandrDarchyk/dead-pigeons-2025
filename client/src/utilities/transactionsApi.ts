import { TransactionsClient } from "@core/generated-client.ts";
import { baseUrl } from "@core/baseUrl.ts";
import { customFetch } from "@utilities/customFetch.ts";

export const transactionsApi = new TransactionsClient(
    baseUrl,
    { fetch: customFetch }
);
