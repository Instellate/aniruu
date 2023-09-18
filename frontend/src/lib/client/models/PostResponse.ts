/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

import type { PostAuthorResponse } from './PostAuthorResponse';
import type { PostRating } from './PostRating';
import type { PostTagsResponse } from './PostTagsResponse';

export type PostResponse = {
    id: number;
    location: string;
    rating: PostRating;
    source?: string;
    tags: Array<PostTagsResponse>;
    createdBy: PostAuthorResponse;
    createdAt: string;
};
