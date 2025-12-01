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
 * Small wrapper around the NSwag-generated PlayersClient.
 *
 * Notes:
 * - We use DTOs (PlayerResponseDto, CreatePlayerRequestDto, UpdatePlayerRequestDto).
 * - customFetch attaches JWT token and handles ProblemDetails from backend.
 */
export const playersApi = new PlayersClient(baseUrl, { fetch: customFetch });

/**
 * Convenience aliases so the rest of the app can import these types
 * directly from "@utilities/playersApi.ts".
 */
export type PlayerDto = PlayerResponseDto;
export type CreatePlayerDto = CreatePlayerRequestDto;
export type UpdatePlayerDto = UpdatePlayerRequestDto;
