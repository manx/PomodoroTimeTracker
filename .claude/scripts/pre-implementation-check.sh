#!/bin/bash
# Pre-Implementation Check Script
# Run this before starting a new feature to check for pending work

set -e

RED='\033[0;31m'
YELLOW='\033[1;33m'
GREEN='\033[0;32m'
NC='\033[0m' # No Color

echo "=== Pre-Implementation Check ==="
echo ""

ISSUES_FOUND=0

# 1. Check uncommitted changes
echo "1. Checking uncommitted changes..."
if [[ -n $(git status --porcelain) ]]; then
    echo -e "${YELLOW}⚠ Uncommitted changes found:${NC}"
    git status --short
    echo ""
    ISSUES_FOUND=1
else
    echo -e "${GREEN}✓ Working directory clean${NC}"
fi
echo ""

# 2. Check open PRs
echo "2. Checking open PRs..."
GH_CMD="${GH_CMD:-gh}"
[[ -f "/c/Program Files/GitHub CLI/gh.exe" ]] && GH_CMD="/c/Program Files/GitHub CLI/gh.exe"

if command -v "$GH_CMD" &> /dev/null || [[ -f "$GH_CMD" ]]; then
    OPEN_PRS=$(gh pr list --state open --json number,title,headRefName 2>/dev/null || echo "[]")
    PR_COUNT=$(echo "$OPEN_PRS" | grep -c '"number"' || echo "0")

    if [[ "$PR_COUNT" -gt 0 ]]; then
        echo -e "${YELLOW}⚠ Open PRs found:${NC}"
        gh pr list --state open
        echo ""
        ISSUES_FOUND=1
    else
        echo -e "${GREEN}✓ No open PRs${NC}"
    fi
else
    echo -e "${YELLOW}⚠ gh CLI not available, skipping PR check${NC}"
fi
echo ""

# 3. Check for existing feature/chore branches not merged to master
echo "3. Checking for existing unmerged branches..."
git fetch origin --quiet 2>/dev/null || true

# Get branches that exist on remote but aren't merged to master
UNMERGED_BRANCHES=$(git branch -r --no-merged origin/master 2>/dev/null | grep -v HEAD | grep -E "(feat|fix|chore|refactor)/" | sed 's/.*origin\///' | tr -d ' ' || true)

if [[ -n "$UNMERGED_BRANCHES" ]]; then
    echo -e "${YELLOW}⚠ Unmerged feature branches found:${NC}"
    for branch in $UNMERGED_BRANCHES; do
        # Get the latest commit info for context
        LAST_COMMIT=$(git log -1 --format="%s (%ar)" "origin/$branch" 2>/dev/null || echo "")
        echo "  - $branch"
        [[ -n "$LAST_COMMIT" ]] && echo "      └─ $LAST_COMMIT"
    done
    echo ""
    echo -e "${YELLOW}  Consider: Is your new work related to any of these branches?${NC}"
    echo ""
    ISSUES_FOUND=1
else
    echo -e "${GREEN}✓ No unmerged feature branches${NC}"
fi
echo ""

# 4. Check if behind origin
echo "4. Checking sync with origin/master..."
BEHIND=$(git rev-list HEAD..origin/master --count 2>/dev/null || echo "0")

if [[ "$BEHIND" -gt 0 ]]; then
    echo -e "${YELLOW}⚠ Behind origin/master by $BEHIND commit(s):${NC}"
    git log HEAD..origin/master --oneline
    echo ""
    ISSUES_FOUND=1
else
    echo -e "${GREEN}✓ Up to date with origin/master${NC}"
fi
echo ""

# Summary
echo "=== Summary ==="
if [[ "$ISSUES_FOUND" -eq 0 ]]; then
    echo -e "${GREEN}✓ All checks passed. Ready to implement!${NC}"
    exit 0
else
    echo -e "${YELLOW}⚠ Issues found. Review above before proceeding.${NC}"
    exit 1
fi
