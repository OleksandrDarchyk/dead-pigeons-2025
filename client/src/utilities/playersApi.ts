// src/utilities/playersApi.ts
import { PlayersClient, type PlayerResponseDto } from "@core/generated-client.ts";
import { baseUrl } from "@core/baseUrl.ts";
import { customFetch } from "@utilities/customFetch.ts";

/**
 * Small wrapper around the NSwag-generated PlayersClient.
 *
 * Security / architecture notes:
 * - We work only with PlayerResponseDto (DTO), not with EF entities.
 * - customFetch attaches the JWT token and handles ProblemDetails responses.
 * - No dotnet-json-refs is needed anymore, because the backend uses
 *   ReferenceHandler.IgnoreCycles and returns a clean JSON array.
 */
export const playersApi = new PlayersClient(baseUrl, { fetch: customFetch });

/**
 * Convenience type alias so the rest of the app can use PlayerDto
 * instead of importing PlayerResponseDto directly from generated-client.
 */
export type PlayerDto = PlayerResponseDto;
