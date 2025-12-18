import {
    BoardClient,
    type BoardResponseDto,
    type CreateBoardRequestDto,
} from "@core/api/generated/generated-client.ts";
import { baseUrl } from "@core/config/baseUrl.ts";
import { customFetch } from "@core/api/customFetch.ts";

export const boardsApi = new BoardClient(baseUrl, { fetch: customFetch });

// Optional DTO aliases
export type BoardDto = BoardResponseDto;
export type CreateBoardDto = CreateBoardRequestDto;
