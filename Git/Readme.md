# Git Command
Helpful advanced git commands

## Git Diff
Open git bash
```
cd /c/Temp/Git/My/
git add .
git diff main > my.diff # Creates diff of all staged files
git apply my.diff
```

## Git Rebase
```
git rebase --onto main
git push --force
```

## Git Undo
Undo commited changes (rewrites history!)
```
git push --force origin 9944d62:mybranch # Set branch mybranch back to commit 9944d62
```
