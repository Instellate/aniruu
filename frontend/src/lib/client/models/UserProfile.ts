/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

import type { UserPermission } from './UserPermission';

export type UserProfile = {
    id: number;
    username: string;
    permission?: UserPermission;
    posts: Array<number>;
};
