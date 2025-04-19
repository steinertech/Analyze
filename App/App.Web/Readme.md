# App.Web

## Init
```
npm install -g @angular/cli@latest
ng new App.Web # CSS, SSR
# https://tailwindcss.com/docs/installation/framework-guides/angular
ng generate component page-home --skip-tests # Update app.routes.ts
ng generate component page-about --skip-tests # Update app.routes.ts
ng generate component page-debug --skip-tests # Update app.routes.ts
ng generate component page-nav --skip-tests # Update app.routes.ts
ng generate service data --skip-tests
npm install -D @tailwindcss/typography # See also https://tailwindcss.com/blog/tailwindcss-typography
npm install @angular/cdk # Used for BreakpointObserver and routerLinkActive
```

## Build

```
ng build
npm install --global http-server@latest
http-server dist/app.web/browser
```
