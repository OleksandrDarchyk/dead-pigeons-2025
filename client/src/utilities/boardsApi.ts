// src/utilities/boardsApi.ts
import { BoardClient } from "@core/generated-client";
import { baseUrl } from "@core/baseUrl";
import { customFetch } from "@utilities/customFetch";

export const boardsApi = new BoardClient(baseUrl, customFetch);
