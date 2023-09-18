<script lang="ts">
    import { client } from '$lib';
    import PostTags from '$lib/PostTags.svelte';
    import { setSidebarContent } from '$lib/stores';
    import type { PageData } from './$types';

    export let data: PageData;

    setSidebarContent({
        component: PostTags,
        data: {
            tags: data.post.tags
        }
    });
</script>

<div class="px-5 py-5">
    <img
        src="{client.request.config.BASE + data.post.location}?size=720"
        alt="Post"
        class="bg-white"
    />
</div>
