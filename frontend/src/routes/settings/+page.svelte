<script lang="ts">
    import { setSidebarContent } from '$lib/stores';
    import type { PageData } from './$types';
    import Test from '$lib/Test.svelte';

    export let data: PageData;

    setSidebarContent({
        data: {
            num: 123
        },
        component: Test
    });
</script>
