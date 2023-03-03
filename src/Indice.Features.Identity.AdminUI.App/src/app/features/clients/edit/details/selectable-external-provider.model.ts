export class SelectableExternalProvider {
    constructor(selected: boolean, displayName?: string | undefined, authenticationScheme?: string | undefined) {
        this.selected = selected;
        this.displayName = displayName;
        this.authenticationScheme = authenticationScheme;
    }

    displayName?: string | undefined;
    authenticationScheme?: string | undefined;
    selected: boolean;
}
