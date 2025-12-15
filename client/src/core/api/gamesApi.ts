// src/utilities/gamesApi.ts
import {
    GamesClient,
    type GameResponseDto,
    type SetWinningNumbersRequestDto,
} from "@core/api/generated/generated-client.ts";
import { baseUrl } from "@core/config/baseUrl.ts";
import { customFetch } from "@core/api/customFetch.ts";

/**
 * NSwag GamesClient instance using customFetch.
 * Methods:
 * - getActiveGame
 * - getGamesHistory
 * - setWinningNumbers
 */
export const gamesApi = new GamesClient(baseUrl, { fetch: customFetch });

// DTO aliases for convenience
export type GameDto = GameResponseDto;
export type SetWinningNumbersDto = SetWinningNumbersRequestDto;
