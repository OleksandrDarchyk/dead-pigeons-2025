import { GamesClient } from "@core/generated-client.ts";
import { baseUrl } from "@core/baseUrl.ts";
import { customFetch } from "@utilities/customFetch.ts";

export const gamesApi = new GamesClient(baseUrl, { fetch: customFetch });
