import { BoardClient } from "@core/generated-client.ts";
import { baseUrl } from "@core/baseUrl.ts";
import { customFetch } from "@utilities/customFetch.ts";

export const boardsApi = new BoardClient(baseUrl, { fetch: customFetch });
