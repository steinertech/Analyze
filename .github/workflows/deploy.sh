#!/bin/bash
set -e
set -x

cd App/App.Web
npm ci
npm run build
cd ../..

cd pages
git rm -r * # Delete all files and folders
cp -r ../App/App.Web/dist/app.web/browser/. . # Copy

git add .
git commit -m "Publish"
git push
cd ..
