/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

import type { PostResponse } from './PostResponse';

export type PostsPage = {
    /**
     * The amount of pages this query has
     */
    total: number;
    posts: Array<PostResponse>;
};
