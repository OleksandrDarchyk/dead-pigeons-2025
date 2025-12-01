// src/utilities/boardsApi.ts
import {
    BoardClient,
    type BoardResponseDto,
    type CreateBoardRequestDto,
} from "@core/generated-client.ts";
import { baseUrl } from "@core/baseUrl.ts";
import { customFetch } from "@utilities/customFetch.ts";

/**
 * NSwag BoardClient instance using customFetch.
 * Methods:
 * - createBoard
 * - getBoardsForGame
 * - getBoardsForPlayer
 * - getMyBoards
 */
export const boardsApi = new BoardClient(baseUrl, { fetch: customFetch });

// Optional DTO aliases
export type BoardDto = BoardResponseDto;
export type CreateBoardDto = CreateBoardRequestDto;
