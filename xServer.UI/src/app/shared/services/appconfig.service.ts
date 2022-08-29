import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { AppConfig } from "./appconfig";

@Injectable({
    providedIn: 'root'
  })
export class AppConfigService {
    private config: AppConfig;
    loaded = false;
    constructor(private http: HttpClient) {}
    loadConfig(): Promise<void> {
        return this.http
            .get<AppConfig>('/assets/app.config.json')
            .toPromise()
            .then(data => {
                this.config = data;
                this.loaded = true;
            });
    }
    
    getConfig(): AppConfig {
        return this.config;
    }
}
