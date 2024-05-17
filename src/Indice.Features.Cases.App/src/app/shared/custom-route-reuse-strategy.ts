import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, RouteReuseStrategy, DetachedRouteHandle } from '@angular/router';
import { CaseTypeCaseListComponent } from '../features/cases/case-type-case-list/case-type-case-list.component';

@Injectable()
export class CustomRouteReuseStrategy implements RouteReuseStrategy {
  shouldDetach(route: ActivatedRouteSnapshot): boolean {
    return false;
  }

  store(route: ActivatedRouteSnapshot, handle: DetachedRouteHandle | null): void {}

  shouldAttach(route: ActivatedRouteSnapshot): boolean {
    return false;
  }

  retrieve(route: ActivatedRouteSnapshot): DetachedRouteHandle | null {
    return null;
  }

  shouldReuseRoute(future: ActivatedRouteSnapshot, curr: ActivatedRouteSnapshot): boolean {
    // Check if the route is CaseTypeCaseListComponent and has different parameters
    if (future.component === CaseTypeCaseListComponent && curr.component === CaseTypeCaseListComponent) {
      return false;
    }
    return future.routeConfig === curr.routeConfig;
  }
}
