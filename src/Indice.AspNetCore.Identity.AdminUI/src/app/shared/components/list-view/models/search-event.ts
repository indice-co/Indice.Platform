export class SearchEvent {
    constructor(public page: number, public pageSize: number, public sortField?: string, public searchTerm?: string) { }
}
