#!/bin/bash
set -e

cd App/App.Web
npm ci
npm run build
cd ../..

cd pages
git rm -r * # Delete all files and folders
cp -r ../App/App.Web/dist/app.web/browser/. . # Copy

git config user.name "${{ github.actor }}"Add commentMore actions
git config user.email "${GITHUB_ACTOR_ID}+${GITHUB_ACTOR}@users.noreply.github.com"

git add .
git commit -m "Publish"
git push
cd ..
