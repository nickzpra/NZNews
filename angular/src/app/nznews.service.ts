import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';

export interface NewsStory {
  // id: number;
  // title: string;
  // link: string;


  by: string;
  descendants: number;
  id: number;
  score: number;
  time: number;
  title: string;
  type: string;
  url: string;
}

export interface PagedStoriesResult {
  stories: NewsStory[];
  totalCount: number;
}

@Injectable({
  providedIn: 'root'
})
export class NewsService {

  private apiUrl = environment.apiUrl;

  //private apiUrl = 'http://localhost:5039/api/News'; // Adjust to your API endpoint
  //private apiUrl = 'https://nznewsapi.azurewebsites.net/api/news'; // Adjust to your API endpoint

  constructor(private http: HttpClient) { }

  getStories(page: number, pageSize: number, smallestId: number, previousPage: boolean, search?: string, storyType?: string): Observable<PagedStoriesResult> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
      // .set('lastItemId', smallestId)
      // .set('previousPage', previousPage);

    if (search) {
      params = params.set('search', search);
    }

    if (storyType) {
      params = params.set('storyType', storyType);
    }

    return this.http.get<PagedStoriesResult>(this.apiUrl, { params });
  }
}