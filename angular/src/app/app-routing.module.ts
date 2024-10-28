import { NgModule } from '@angular/core';
import { NavigationEnd, NavigationStart, Router, RouterModule, Routes } from '@angular/router';
import { NZNewsComponent } from './nznews/nznews.component';
import { AppComponent } from './app.component';


const routes: Routes = [
    { path: '', redirectTo: '/nznews', pathMatch: 'full' }, // Redirect to news
    { path: 'nznews', component: NZNewsComponent },
    // Add more routes here as needed
];

@NgModule({
    imports: [
        RouterModule.forRoot(routes),
        RouterModule.forChild([
            {
                path: 'app',
                component: AppComponent,
                children: [
                    {
                        path: '',
                        children: [
                            { path: 'nznews', component: NZNewsComponent },
                            { path: '', redirectTo: '/app/nznews', pathMatch: 'full' },
                        ],
                    },
                    {
                        path: '**',
                        redirectTo: 'notifications',
                    },
                ],
            },
        ]),
    ],
    exports: [RouterModule]
})
export class AppRoutingModule {
    constructor(private router: Router) {
        router.events.subscribe((event) => {
            if (event instanceof NavigationStart) {
                //spinnerService.show();
            }

            if (event instanceof NavigationEnd) {
                //document.querySelector('meta[property=og\\:url').setAttribute('content', window.location.href);
                //spinnerService.hide();
            }
        });
    }
}