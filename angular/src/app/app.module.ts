import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms'; // Import FormsModule for ngModel
import { HttpClientModule } from '@angular/common/http';
import { AppRoutingModule } from './app-routing.module'; // Import routing module
import { AppComponent } from './app.component';
import { NZNewsComponent } from './nznews/nznews.component';
import { CommonModule } from '@angular/common'; // Import CommonModule

// Import PrimeNG modules
import { TableModule } from 'primeng/table'; // <-- Import the TableModule
import { PaginatorModule } from 'primeng/paginator';
import { DropdownModule } from 'primeng/dropdown';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';

@NgModule({
    declarations: [
        AppComponent,
        NZNewsComponent,
    ],
    imports: [
        BrowserModule,
        BrowserAnimationsModule,
        FormsModule,
        DropdownModule,
        HttpClientModule,
        AppRoutingModule,
        CommonModule,
        TableModule,
        PaginatorModule, 
    ],
    providers: [],
    bootstrap: [AppComponent]
})
export class AppModule {}