import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SidePaneHostComponent } from './layout/side-pane-host.component';
import { ThemeService } from './layout/theme.service';
import { ToasterComponent } from './ui/toast/toaster.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, SidePaneHostComponent, ToasterComponent],
  template: `
    <router-outlet />
    <app-side-pane-host />
    <app-toaster />
  `
})
export class AppComponent {
  constructor() {
    // apply the stored theme everywhere, including pages outside the shell
    inject(ThemeService);
  }
}
