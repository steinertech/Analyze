#!/bin/bash
set -e
set -x

cd App/App.Web
npm ci
ng build --localize
cd ../..

cd pages
git rm -r * # Delete all files and folders
cp -r ../App/App.Web/dist/App.Web/browser/. . # Copy

git add .
git commit -m "Publish"
git push
cd ..
