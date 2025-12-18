import {
    AuthClient,
    type JwtResponse,
    type LoginRequestDto,
    type RegisterRequestDto,
} from "@core/api/generated/generated-client.ts";
import { baseUrl } from "@core/config/baseUrl.ts";
import { customFetch } from "@core/api/customFetch.ts";

const client = new AuthClient(baseUrl, { fetch: customFetch });

export const authApi = {
    login(dto: LoginRequestDto): Promise<JwtResponse> {
        return client.login(dto);
    },

    register(dto: RegisterRequestDto): Promise<JwtResponse> {
        return client.register(dto);
    },

    whoAmI() {
        return client.whoAmI();
    },
};
export type { LoginRequestDto, RegisterRequestDto, JwtResponse };
