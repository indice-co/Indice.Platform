import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

@Component({
    selector: 'app-http-status',
    templateUrl: './http-status.component.html'
})
export class HttpStatusComponent implements OnInit {
    constructor(private route: ActivatedRoute) { }

    public code: string = '-';
    public title: string = '-';
    public message: string = '-';

    public ngOnInit(): void {
        this.route.data.subscribe(data => {
            this.code = data.code || '404';
            this.title = data.title;
            this.message = data.message;
        });
    }
}
