export class SampleViewModel {
    public title: string | undefined;
    public description: string | undefined;
    public image: string | undefined;
    public path: string | undefined;

    constructor(title: string | undefined, description: string | undefined, image: string | undefined, path: string | undefined) {
        this.title = title;
        this.description = description;
        this.image = image;
        this.path = path;
    }
}
