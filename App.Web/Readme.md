# App.Web

## Init

```
npm install -g @angular/cli@latest
ng new App.Web
ng add @angular/localize
# https://tailwindcss.com/docs/guides/angular
ng generate service data --skip-tests
ng generate component page-home --skip-tests # Update app.routes.ts
ng generate component page-debug --skip-tests # Update app.routes.ts
ng generate class generate --skip-tests
```

## Build

```
cd App.Web
ng extract-i18n --format=json # Then copy generated file messages.json to messages.de.json

ng build --localize # Then move generated folder dist/app/browser/en-US to dist/app/browser
xcopy.exe dist\app.web\browser\en-US\*.* dist\app.web\browser\ /E
del dist/app.web/browser/en-US/
npm install --global http-server@latest
http-server dist/app.web/browser
```
