<script lang="ts">
    import { hideSidebar, userStore } from '$lib/stores';
    import { FileDropzone } from '@skeletonlabs/skeleton';
    import { ApiError, type CreateBody, type PostCreated } from '$lib/client';
    import { goto } from '$app/navigation';
    import { env } from '$env/dynamic/public';

    let tags: string;
    let rating: number;
    let source: string;
    let files: FileList;
    let img: { url: string; filename: string } | null = null;
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
                const request = await fetch(env.PUBLIC_API_URI + '/api/post', {
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

    function fileDrop() {
        const file = files.item(0);
        if (file) {
            if (img) {
                URL.revokeObjectURL(img.url);
            }
            img = {
                url: URL.createObjectURL(file),
                filename: file.name
            };
        }
    }

    hideSidebar();
</script>

<svelte:head>
    <title>Upload a post | {env.PUBLIC_TITLE}</title>
</svelte:head>

{#if $userStore === null}
    <div class="flex items-center justify-center h-full">
        <strong class="text-xl"> You aren't logged in </strong>
    </div>
{:else}
    <div class="flex justify-center mx-auot items-center h-full">
        <div
            class="flex flex-col items-center bg-surface-900 rounded gap-6 p-4 lg:p-16 lg:w-2/5 w-[90%]"
        >
            <FileDropzone name="files" bind:files on:change={fileDrop}>
                <svelte:fragment slot="lead">
                    {#if img}
                        <img
                            src={img.url}
                            alt="Preview of {img.filename}"
                            class="w-fit max-h-96 h-fit rounded-sm object-contain"
                        />
                    {/if}
                </svelte:fragment>
                <svelte:fragment slot="message">
                    <span>
                        <strong>Click here to upload a file</strong> or drag and drop
                    </span>
                </svelte:fragment>
            </FileDropzone>
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
            />
            <!--TODO: Make this better-->
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
