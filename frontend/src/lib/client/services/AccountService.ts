/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { AuthUri } from '../models/AuthUri';
import type { ClaimUsername } from '../models/ClaimUsername';
import type { SessionCreated } from '../models/SessionCreated';

import type { CancelablePromise } from '../core/CancelablePromise';
import type { BaseHttpRequest } from '../core/BaseHttpRequest';

export class AccountService {
    constructor(public readonly httpRequest: BaseHttpRequest) {}

    /**
     * OAuth2 Callback
     * @param code The OAuth2 code
     * @param state The OAuth2 state
     * @returns void
     * @throws ApiError
     */
    public accountRedirectUrl(code?: string, state?: string): CancelablePromise<void> {
        return this.httpRequest.request({
            method: 'GET',
            url: '/api/Account/redirect',
            query: {
                code: code,
                state: state
            }
        });
    }

    /**
     * Get Authentication URI's
     * @returns AuthUri All the URI's for authentication
     * @throws ApiError
     */
    public accountGetAuthUri(): CancelablePromise<Array<AuthUri>> {
        return this.httpRequest.request({
            method: 'GET',
            url: '/api/Account/authUris'
        });
    }

    /**
     * Claim username
     * @param body
     * @returns SessionCreated
     * @throws ApiError
     */
    public accountClaimUsername(body: ClaimUsername): CancelablePromise<SessionCreated> {
        return this.httpRequest.request({
            method: 'POST',
            url: '/api/Account/claimUsername',
            body: body
        });
    }

    /**
     * Delete session
     * @param id The id for the session
     * @returns void
     * @throws ApiError
     */
    public accountDeleteSession(id: string): CancelablePromise<void> {
        return this.httpRequest.request({
            method: 'DELETE',
            url: '/api/Account/session/{id}',
            path: {
                id: id
            }
        });
    }
}
