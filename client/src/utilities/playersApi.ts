import {PlayersClient, type Player} from "@core/generated-client.ts";
import {baseUrl} from "@core/baseUrl.ts";
import {customFetch} from "@utilities/customFetch.ts";
import {resolveRefs} from "dotnet-json-refs";

// Small wrapper around PlayersClient that resolves circular JSON references
class PlayersClientWithResolvedRefs extends PlayersClient {
    override async getPlayers(isActive?: boolean | null): Promise<Player[]> {
        const result = await super.getPlayers(isActive);
        return resolveRefs(result);
    }
}

// Main API instance for working with players
export const playersApi = new PlayersClientWithResolvedRefs(baseUrl, customFetch);
