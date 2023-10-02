/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { UserProfile } from '../models/UserProfile';

import type { CancelablePromise } from '../core/CancelablePromise';
import type { BaseHttpRequest } from '../core/BaseHttpRequest';

export class UserService {
    constructor(public readonly httpRequest: BaseHttpRequest) {}

    /**
     * @returns UserProfile
     * @throws ApiError
     */
    public userGetUserMe(): CancelablePromise<UserProfile> {
        return this.httpRequest.request({
            method: 'GET',
            url: '/api/User/me'
        });
    }

    /**
     * @param id
     * @returns void
     * @throws ApiError
     */
    public userGetUser(id: number): CancelablePromise<void> {
        return this.httpRequest.request({
            method: 'GET',
            url: '/api/User/{id}',
            path: {
                id: id
            }
        });
    }
}
