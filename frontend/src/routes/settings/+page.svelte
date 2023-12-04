<script lang="ts">
    import { setSidebarContent } from '$lib/stores';
    import type { PageData } from './$types';
    import Test from '$lib/Test.svelte';
    import { env } from '$env/dynamic/public';

    export let data: PageData;

    setSidebarContent({
        data: {
            num: 123
        },
        component: Test
    });
</script>

<svelte:head>
    <title>Settings | {env.PUBLIC_TITLE}</title>
</svelte:head>
