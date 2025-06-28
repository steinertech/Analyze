#!/bin/bash
cd App/App.Web
npm ci
npm run build
cd ../..

cd pages
echo "Hello" > a.txt
git config user.name "SteinerTech"
git config user.email "205841367+steinertech@users.noreply.github.com" # https://github.com/settings/emails
git add .
git commit -m "Publish"
git push
cd ..
