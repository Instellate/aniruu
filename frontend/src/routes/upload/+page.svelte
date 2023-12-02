<script lang="ts">
    import { hideSidebar, userStore } from '$lib/stores';
    import { FileDropzone } from '@skeletonlabs/skeleton';
    import { ApiError, type CreateBody, type PostCreated } from '$lib/client';
    import { goto } from '$app/navigation';

    let tags: string;
    let rating: number;
    let source: string;
    let files: FileList;
    let isUploading = false;

    async function uploadFile(): Promise<void> {
        if (isUploading) {
            return;
        } else {
            isUploading = true;
        }

        const file = files.item(0);
        if (file !== null) {
            const formData = new FormData();

            const jsonBody: CreateBody = {
                tags: tags,
                rating: Number(rating),
                source: source
            };
            formData.append('body', JSON.stringify(jsonBody));
            formData.append('file', file);

            try {
                const request = await fetch(import.meta.env.VITE_API_URI + '/api/post', {
                    method: 'POST',
                    body: formData,
                    headers: {
                        Authorization: `Bearer ${$userStore?.sessionToken}`
                    }
                });
                const createPost: PostCreated = await request.json();
                await goto(`/post/${createPost.postId}`);
            } catch (err: unknown) {
                if (err instanceof ApiError) {
                    return;
                }
            }
        }
        isUploading = false;
        return;
    }

    hideSidebar();
</script>

<svelte:head>
    <title>Upload a post</title>
</svelte:head>

{#if $userStore === null}
    <div class="flex items-center justify-center h-full">
        <strong class="text-xl"> You aren't logged in </strong>
    </div>
{:else}
    <div class="flex justify-center mx-auot items-center h-full">
        <div class="flex flex-col items-center bg-surface-900 rounded gap-6 p-16 w-2/5">
            <FileDropzone name="files" bind:files />
            <textarea class="textarea" rows="4" placeholder="Tags..." bind:value={tags} />
            <select class="select" placeholder="Choose a rating..." bind:value={rating}>
                <option value="0">Safe</option>
                <option value="1">Questionable</option>
                <option value="2">Explicit</option>
            </select>
            <input
                type="text"
                class="input variant-form-material"
                placeholder="Source"
                bind:value={source}
            /> <!--TODO: Make this better-->
            <button
                class="btn variant-ghost-surface w-32"
                disabled={isUploading}
                on:click={uploadFile}
            >
                Upload
            </button>
        </div>
    </div>
{/if}
