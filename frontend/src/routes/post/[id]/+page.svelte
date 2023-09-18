<script lang="ts">
    import { client } from '$lib';
    import PostTags from '$lib/PostTags.svelte';
    import { preference, setSidebarContent, userStore } from '$lib/stores';
    import type { PageData } from './$types';

    export let data: PageData;

    setSidebarContent({
        component: PostTags,
        data: {
            tags: data.post.tags
        }
    });

    let imgUrl = $preference.prefersOriginalSize
            ? `${client.request.config.BASE + data.post.location}`
            : `${client.request.config.BASE + data.post.location}?size=720`;
    function changeImgSize() {
        imgUrl = imgUrl.endsWith('720')
            ? `${client.request.config.BASE + data.post.location}`
            : `${client.request.config.BASE + data.post.location}?size=720`;
    }
</script>

<div class="px-5 py-5">
    <button on:click={changeImgSize}>
        <img src={imgUrl} alt="Post" class="bg-white" />
    </button>
</div>
