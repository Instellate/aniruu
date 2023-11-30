<script lang="ts">
    import { goto } from '$app/navigation';
    import { client } from '$lib';
    import {
        Autocomplete,
        type AutocompleteOption,
        type PopupSettings,
        popup
    } from '@skeletonlabs/skeleton';
    import debounce from 'lodash.debounce';

    let options: AutocompleteOption[] = [];

    const popupSettings: PopupSettings = {
        event: 'focus-click',
        target: 'popupAutocomplete',
        placement: 'bottom'
    };

    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    let htmlInput: HTMLInputElement = null!;
    let input = '';
    let manipulatedInput = '';

    const onInputChange = debounce(async () => {
        if (input === '') return;

        let tag = input.split(' ').pop();
        if (tag === undefined || tag.trim() === '') {
            manipulatedInput = '';
            if (htmlInput !== null) htmlInput.focus();
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

            if (htmlInput !== null) htmlInput.focus();
    }, 300);

    function onSelect(event: CustomEvent<AutocompleteOption>): void {
        let arr = input.split(' ');
        arr[arr.length - 1] = event.detail.label;
        input = arr.join(' ') + ' ';
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
</script>

<input
    class="input autocomplete"
    type="search"
    name="autocomplete-search"
    placeholder="Search..."
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
