import { Component, OnInit, ViewChild } from '@angular/core';
import { NewsService, NewsStory } from '../nznews.service';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-nznews',
  standalone: true,
  imports: [FormsModule, HttpClientModule, CommonModule, TableModule, PaginatorModule],
  templateUrl: './nznews.component.html',

})
export class NZNewsComponent implements OnInit {

  options: { label: string, value: string }[] = [
    { label: 'Top', value: 'top' },
    { label: 'New', value: 'new' },
    { label: 'Best', value: 'best' },
    { label: 'Show', value: 'show' },
    { label: 'Job', value: 'job' }
  ];

  selectedOption: string = '';
  stories: NewsStory[] = [];
  search: string = '';
  storyType: string = '';
  page: number = 1;
  pageSize: number = 10;
  totalRecords: number = 0;
  firstPage: number = 0;
  lastItemId: number = -1;
  previousPage: boolean = false;

  nextPage: boolean = false;

  loading: boolean = false;
  lastPage: number = -1;

  @ViewChild('dataTable', { static: true }) dataTable!: Table; // Initialize as undefined
  @ViewChild('paginator', { static: true }) paginator: Paginator | undefined; // Initialize as undefined



  constructor(private nznewsService: NewsService) { }
  ngOnInit(): void {
    //this.loadStories();
  }

  ngAfterViewInit() {
    //this.loadStories();
  }

  loadStories() {
    this.loading = true;
    const ar = this.stories.map(story => story.id);
    if(ar.length)
    {
      if(this.previousPage)
      {
        this.lastItemId = Math.max(...ar).valueOf();
      }
      else if(this.nextPage){
        this.lastItemId = Math.min(...ar).valueOf();
      }
    }

    this.nznewsService.getStories(this.page, this.pageSize, this.lastItemId, this.previousPage, this.search, this.storyType).subscribe(result => {
      this.stories = result.stories; // Populate the data array with API response
      this.dataTable.value = this.stories; // Populate the table
      this.totalRecords = result.totalCount;
      this.lastItemId = -1;
      this.previousPage = false;
      this.nextPage = false;
      this.loading = false;
    }, error => {
      console.error('Error fetching data', error); // Handle errors appropriately
      this.loading = false;
      this.lastItemId = -1;
    });


  }

  searchStories() {
    this.stories = [];
    this.page = 1; // Reset to first page
    this.loadStories();
  }

  nextPageClicked() {
    this.nextPage = true;
    this.page++;
    this.loadStories();
  }

  prevPageClicked() {
    if (this.page > 1) {
      this.page--;
      this.previousPage = true;
      this.loadStories();
    }
  }

  // Handle the pagination change
  paginate(event: any) {

    this.lastPage = this.page;
    this.page = event.page + 1; // The current page number - zero based
    this.pageSize = event.rows; // Number of rows per page

    if(this.lastPage < this.page)
      {
        this.nextPage = true;
      }
      else if(
        this.lastPage > this.page
      )
      {
        this.previousPage = true;
      }

    this.loadStories();

  }

  onChange(event: any) {
    this.storyType = event.value;
  }

  reset() {
    this.stories = [];
    this.totalRecords = 0;
    this.page = 1;
    this.firstPage = 0;
    this.search = '';
    this.storyType = '';
    this.previousPage = false;
    this.nextPage = false;

  }
}