import { PlayersClient, type Player } from "@core/generated-client.ts";
import { baseUrl } from "@core/baseUrl.ts";
import { customFetch } from "@utilities/customFetch.ts";
import { resolveRefs } from "dotnet-json-refs";

class PlayersClientWithResolvedRefs extends PlayersClient {
    override async getPlayers(isActive?: boolean | null): Promise<Player[]> {
        const result = await super.getPlayers(isActive);
        return resolveRefs(result);
    }
}

export const playersApi = new PlayersClientWithResolvedRefs(
    baseUrl,
    { fetch: customFetch }
);
