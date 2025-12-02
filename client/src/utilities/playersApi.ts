// src/utilities/playersApi.ts
import {
    PlayersClient,
    type PlayerResponseDto,
    type CreatePlayerRequestDto,
    type UpdatePlayerRequestDto,
} from "@core/generated-client.ts";
import { baseUrl } from "@core/baseUrl.ts";
import { customFetch } from "@utilities/customFetch.ts";

/**
 * Internal NSwag client instance.
 * customFetch attaches JWT token and handles ProblemDetails from backend.
 */
const client = new PlayersClient(baseUrl, { fetch: customFetch });

/**
 * Small wrapper around the NSwag-generated PlayersClient.
 *
 * We expose only the methods we actually use in the app,
 * with a slightly nicer TypeScript signature.
 */
export const playersApi = {
    /**
     * Get list of players from backend.
     *
     * - isActive: true / false / null (no filter)
     * - sortBy: "fullName" | "email" | "isActive" | "activatedAt" | "createdAt" | null
     * - direction: "asc" | "desc" | null
     */
    getPlayers(
        isActive: boolean | null = null,
        sortBy?: string | null,
        direction?: string | null
    ) {
        // NSwag-метод getPlayers очікує (isActive, sortBy, direction)
        return client.getPlayers(
            isActive,
            sortBy ?? null,
            direction ?? null
        );
    },

    /**
     * Create a new player.
     */
    createPlayer(dto: CreatePlayerRequestDto) {
        return client.createPlayer(dto);
    },

    /**
     * Update existing player.
     */
    updatePlayer(dto: UpdatePlayerRequestDto) {
        return client.updatePlayer(dto);
    },

    /**
     * Activate player by id.
     */
    activatePlayer(playerId: string) {
        return client.activatePlayer(playerId);
    },

    /**
     * Deactivate player by id.
     */
    deactivatePlayer(playerId: string) {
        return client.deactivatePlayer(playerId);
    },

    /**
     * Soft-delete player by id.
     */
    deletePlayer(playerId: string) {
        return client.deletePlayer(playerId);
    },

    /**
     * Get single player by id.
     */
    getPlayerById(playerId: string) {
        return client.getPlayerById(playerId);
    },
};

/**
 * Convenience aliases so the rest of the app can import these types
 * directly from "@utilities/playersApi.ts".
 */
export type PlayerDto = PlayerResponseDto;
export type CreatePlayerDto = CreatePlayerRequestDto;
export type UpdatePlayerDto = UpdatePlayerRequestDto;
