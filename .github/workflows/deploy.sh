#!/bin/bash
set -e
set -x

cd App/App.Web
npm ci
npm run buildLocalize
cd ../..

cd pages
content=$(<CNAME) # Read CNAME file to preserve
git rm -r * # Delete all files and folders
echo "$content" > CNAME # Write CNAME file
cp -r ../App/App.Web/dist/App.Web/browser/. . # Copy

git add .
git commit -m "Publish"
git push
cd ..
