import {
    PlayersClient,
    type PlayerResponseDto,
    type CreatePlayerRequestDto,
    type UpdatePlayerRequestDto,
} from "@core/api/generated/generated-client.ts";
import { baseUrl } from "@core/config/baseUrl.ts";
import { customFetch } from "@core/api/customFetch.ts";

const client = new PlayersClient(baseUrl, { fetch: customFetch });

export const playersApi = {

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

    createPlayer(dto: CreatePlayerRequestDto) {
        return client.createPlayer(dto);
    },

    updatePlayer(dto: UpdatePlayerRequestDto) {
        return client.updatePlayer(dto);
    },

    activatePlayer(playerId: string) {
        return client.activatePlayer(playerId);
    },

    deactivatePlayer(playerId: string) {
        return client.deactivatePlayer(playerId);
    },

    deletePlayer(playerId: string) {
        return client.deletePlayer(playerId);
    },

    getPlayerById(playerId: string) {
        return client.getPlayerById(playerId);
    },
};


export type PlayerDto = PlayerResponseDto;
export type CreatePlayerDto = CreatePlayerRequestDto;
export type UpdatePlayerDto = UpdatePlayerRequestDto;
