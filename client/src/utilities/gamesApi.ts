// src/utilities/gamesApi.ts
import { GamesClient } from "@core/generated-client";
import { baseUrl } from "@core/baseUrl";
import { customFetch } from "@utilities/customFetch";

export const gamesApi = new GamesClient(baseUrl, customFetch);
