import {
    GamesClient,
    type GameResponseDto,
    type SetWinningNumbersRequestDto,
} from "@core/api/generated/generated-client.ts";
import { baseUrl } from "@core/config/baseUrl.ts";
import { customFetch } from "@core/api/customFetch.ts";

export const gamesApi = new GamesClient(baseUrl, { fetch: customFetch });

export type GameDto = GameResponseDto;
export type SetWinningNumbersDto = SetWinningNumbersRequestDto;
