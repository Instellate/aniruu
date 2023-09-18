/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { EditPostBody } from '../models/EditPostBody';
import type { PostCreated } from '../models/PostCreated';
import type { PostResponse } from '../models/PostResponse';

import type { CancelablePromise } from '../core/CancelablePromise';
import type { BaseHttpRequest } from '../core/BaseHttpRequest';

export class PostService {
    constructor(public readonly httpRequest: BaseHttpRequest) {}

    /**
     * @param contentType
     * @param contentDisposition
     * @param headers
     * @param length
     * @param name
     * @param fileName
     * @param body
     * @returns PostCreated
     * @throws ApiError
     */
    public postCreatePost(
        contentType?: string,
        contentDisposition?: string,
        headers?: any,
        length?: number,
        name?: string,
        fileName?: string,
        body?: any
    ): CancelablePromise<PostCreated> {
        return this.httpRequest.request({
            method: 'POST',
            url: '/api/Post',
            query: {
                body: body
            },
            formData: {
                ContentType: contentType,
                ContentDisposition: contentDisposition,
                Headers: headers,
                Length: length,
                Name: name,
                FileName: fileName
            }
        });
    }

    /**
     * @param page
     * @param tags
     * @returns PostResponse
     * @throws ApiError
     */
    public postGetPosts(
        page: number = 1,
        tags?: Array<string> | null
    ): CancelablePromise<Array<PostResponse>> {
        return this.httpRequest.request({
            method: 'GET',
            url: '/api/Post',
            query: {
                page: page,
                tags: tags
            }
        });
    }

    /**
     * @param id
     * @param size
     * @returns binary
     * @throws ApiError
     */
    public postGetImage(id: number, size?: string | null): CancelablePromise<Blob> {
        return this.httpRequest.request({
            method: 'GET',
            url: '/api/Post/{id}',
            path: {
                id: id
            },
            query: {
                size: size
            }
        });
    }

    /**
     * @param id
     * @returns PostResponse
     * @throws ApiError
     */
    public postGetImageData(id: number): CancelablePromise<PostResponse> {
        return this.httpRequest.request({
            method: 'GET',
            url: '/api/Post/{id}/data',
            path: {
                id: id
            }
        });
    }

    /**
     * @param tag
     * @returns string
     * @throws ApiError
     */
    public postSearchTags(tag?: string): CancelablePromise<Array<string>> {
        return this.httpRequest.request({
            method: 'GET',
            url: '/api/Post/post/tags',
            query: {
                tag: tag
            }
        });
    }

    /**
     * @param id
     * @param body
     * @returns binary
     * @throws ApiError
     */
    public postEditPost(id: number, body: EditPostBody): CancelablePromise<Blob> {
        return this.httpRequest.request({
            method: 'PATCH',
            url: '/api/Post/post/{id}',
            path: {
                id: id
            },
            body: body
        });
    }
}
