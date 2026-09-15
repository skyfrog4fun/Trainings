# Developer Cheat Sheet

Quick command reference for common local development and deployment operations.

## Git

### Repository location

Always run Git commands from `D:\Projects\GitHub\TrainingApp\Trainings`.

### Fetch and view remote branches

```bash
git fetch
git branch -a      # all branches (local and remote)
git branch -r      # remote branches only
```

### Branch notation

- `* branch-name`: currently checked out branch
- `branch-name` (no `*`): local branch
- `remotes/origin/branch-name`: remote branch

### Check out a remote branch (creates local tracking branch)

```bash
git checkout {no}-branch-name
```

### Delete a local branch

```bash
git branch -d branch-name   # safe delete (warns on unmerged changes)
git branch -D branch-name   # force delete
```

### Full task cycle (create branch → PR → cleanup)

```bash
# 1. Create the issue (or use the `start-new-task` AI skill)
gh issue create --title "<title>" --body "<body>"

# 2. Sync main and create the feature branch (NN = issue number)
git checkout main
git pull origin main
git checkout -b NN-short-desc

# 3. Work, commit, push
git add <files>
git commit -m "<message>"
git push -u origin NN-short-desc

# 4. Open the PR (or use the `pr-readiness` AI skill, which also
#    runs the quality gate and an automated review pass first)
gh pr create --base main --title "<title>" --body "<body>"

# 5. Review + merge happens on GitHub (human approval required)

# 6. After the PR is merged, clean up locally
git checkout main
git pull origin main
git branch -d NN-short-desc
```

## Docker

### Manual publish to NAS

SSH into the NAS and run:

```bash
cd /volume1/docker/trainings
sudo docker compose pull
sudo docker compose up -d --remove-orphans
sudo docker image prune -f
```

### Restart the container

```bash
cd /volume1/docker/trainings
sudo docker compose restart trainings-web
```

### Check application logs

```bash
# Show the last 100 log lines
sudo docker logs trainings-web --tail 100

# Follow logs in real time (Ctrl+C to stop)
sudo docker logs trainings-web --follow
```