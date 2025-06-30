# App.Web
Multi language angular app.

## Init
```
npm install -g @angular/cli@latest
ng new App.Web # CSS, SSR
ng add @angular/localize
# https://tailwindcss.com/docs/installation/framework-guides/angular
ng generate service data --skip-tests
ng generate component page-home --skip-tests # Update app.routes.ts
ng generate component page-about --skip-tests # Update app.routes.ts
ng generate class generate --skip-tests
npm install -D @tailwindcss/typography # Used for example for h1 and  See also https://tailwindcss.com/blog/tailwindcss-typography
npm install @angular/cdk # Used for BreakpointObserver and routerLinkActive
```

angular.json # For list of locales see https://app.unpkg.com/@angular/common@19.2.0/files/locales
```
  "projects": {
    "App.Web": {
      "i18n": {
        "sourceLocale": {
          "code": "en-US",
          "subPath": ""
        },
        "locales": {
          "de": {
            "subPath": "de",
            "translation": "messages.de.json"
          }
        }
      },
```

## Init (Prettier)
See also https://tailwindcss.com/docs/editor-setup
* Install VS Code extension Tailwind CSS IntelliSense for class autocomplete
* Install VS Code extension https://prettier.io/ for class sorting
```
npm install -D prettier prettier-plugin-tailwindcss # See also https://github.com/tailwindlabs/prettier-plugin-tailwindcss
```
Create file App.Web\.prettierrc.json
```
{
  "plugins": ["prettier-plugin-tailwindcss"]
}
```
**Format Html Code:** Press F1 > Format Document With > Prettier - Code formatter

## Build

```
cd App.Web
ng extract-i18n --format=json # Then copy generated file messages.json to messages.de.json
ng build --localize
ng build --localize --base-href "/analyze/app.web/ # If hosted on sub domain
npm install --global http-server@latest
http-server dist/app.web/browser
```

## Publish
Copy dist\app.web\browser to for example GitHub Pages

## Favicon
https://www.xiconeditor.com/

## Typography
https://tailwindcss.com/blog/tailwindcss-typography