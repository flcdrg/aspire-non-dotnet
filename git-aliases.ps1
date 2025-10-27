git config --global alias.default-branch "!git symbolic-ref refs/remotes/origin/HEAD | sed 's@^refs/remotes/origin/@@'"
git config --global alias.merge-base-origin '!f() { git merge-base ${1-HEAD} origin/$(git default-branch); };f '
git config --global alias.stack '!f() { BRANCH=${1-HEAD}; MERGE_BASE=$(git merge-base-origin $BRANCH); git log --decorate-refs=refs/heads --simplify-by-decoration --pretty=format:\"%(decorate:prefix=,suffix=,tag=,separator=%n)\" $MERGE_BASE..$BRANCH; };f '
git config --global alias.push-stack '!f() { BRANCH=${1-HEAD};  git stack $BRANCH | xargs -I {} git push --force-with-lease origin {}; };f '
