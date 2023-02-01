export class MenuItem {
    constructor(
        public title: string,
        public path: string,
        public visible: boolean,
        public iconName?: string | undefined,
        public isOpen?: boolean | undefined,
        public children?: MenuItem[] | undefined
    ) { }

    public hasChildren(): boolean {
        return this.children && this.children.length > 0;
    }

    public toggle() {
        this.isOpen = !this.isOpen;
    }
}
