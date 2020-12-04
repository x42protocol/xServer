import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'sizeUnit'
})
export class SizeUnitPipe implements PipeTransform {

  private units = ['bytes', 'KB', 'MB', 'GB'];

  transform(value: number, precision: number = 1): string {
    if (typeof value === 'number') {
      if (isNaN(parseFloat(String(value))) || !isFinite(value)) { return '?'; }

      let unit = 0;

      while (value >= 1024) {
        value /= 1024;
        unit++;
      }

      return (value.toFixed(+precision) + ' ' + this.units[unit]);
    }
  }
}


