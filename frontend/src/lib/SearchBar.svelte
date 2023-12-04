<script lang="ts">
    import { browser } from '$app/environment';
    import { afterNavigate, goto } from '$app/navigation';
    import { client } from '$lib';
    import {
        Autocomplete,
        type AutocompleteOption,
        type PopupSettings,
        popup
    } from '@skeletonlabs/skeleton';
    import debounce from 'lodash.debounce';
    import { onMount } from 'svelte';

    let options: AutocompleteOption[] = [];

    const popupSettings: PopupSettings = {
        event: 'focus-click',
        target: 'popupAutocomplete',
        placement: 'bottom'
    };

    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    let htmlInput: HTMLInputElement = null!;
    let input = '';

    function getAllTags(): string {
        return new URL(document.location.toString()).searchParams
            .getAll('tags')
            .join(' ');
    }

    if (browser) {
        input = getAllTags();
    }

    afterNavigate(() => (input = getAllTags()));

    let manipulatedInput = '';

    const onInputChange = debounce(async () => {
        if (input === '') return;

        let tag = input.split(' ').pop();
        if (tag === undefined || tag.trim() === '') {
            manipulatedInput = '';
            return;
        }

        manipulatedInput = tag.startsWith('-') ? tag.slice(1) : tag;

        let tags = await client.post.postSearchTags(manipulatedInput);
        options = tags
            .sort((a, b) => a.localeCompare(b))
            .map((t) => {
                t = tag?.startsWith('-') ? '-' + t : t;
                return {
                    label: t,
                    value: t
                };
            });
    }, 300);

    function onSelect(event: CustomEvent<AutocompleteOption>): void {
        let arr = input.split(' ');
        arr[arr.length - 1] = event.detail.label;
        input = arr.join(' ') + ' ';

        setTimeout(() => {
            htmlInput.focus();
            htmlInput.selectionStart = htmlInput.selectionEnd = 10000;
        }, 200);
    }

    async function onKeyup(event: KeyboardEvent): Promise<void> {
        if (event.key == 'Enter') {
            const splitOutput = input.trim().split(' ');
            const params = new URLSearchParams();

            splitOutput.forEach((t) => params.append('tags', t));
            await goto(`/?${params.toString()}`);

            const elem = document.elementFromPoint(0, 0) as HTMLButtonElement;
            elem.click();
        }
    }

    onMount(() => {
        htmlInput.blur();
        const elem = document.elementFromPoint(0, 0) as HTMLButtonElement;
        elem.click();
    });
</script>

<input
    class="input autocomplete"
    type="search"
    name="autocomplete-search"
    placeholder="Search..."
    tabindex="0"
    use:popup={popupSettings}
    bind:value={input}
    bind:this={htmlInput}
    on:keyup={onKeyup}
    on:input={onInputChange}
/>
<div
    data-popup="popupAutocomplete"
    class="card w-full max-w-sm max-h-48 p-4 overflow-y-auto"
>
    <Autocomplete bind:input={manipulatedInput} {options} on:selection={onSelect} />
</div>
