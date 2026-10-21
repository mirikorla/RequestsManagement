import { Component } from '@angular/core';
import { RequestsPageComponent } from './components/requests-page/requests-page.component';

@Component({
  selector: 'app-root',
  imports: [RequestsPageComponent],
  template: '<app-requests-page />',
})
export class AppComponent {}
