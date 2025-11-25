# Branch Protection Setup Instructions

Detta dokument beskriver hur du konfigurerar branch protection rules för `master` branch i GitHub.

## 📍 Varför Branch Protection?

Branch protection rules säkerställer att:
- ✅ All kod går genom Pull Requests
- ✅ CI/CD måste vara grön innan merge
- ✅ Ingen kan force-pusha till master
- ✅ Master är alltid stabil och deploybar
- ✅ Code review blir en naturlig del av processen

## 🚀 Setup Instructions

### Steg 1: Navigera till Repository Settings

1. Gå till ditt GitHub repository
2. Klicka på **Settings** (högst upp till höger)
3. I vänstermenyn, klicka på **Branches** under "Code and automation"

### Steg 2: Lägg till Branch Protection Rule

1. Under "Branch protection rules", klicka på **Add branch protection rule**
2. I "Branch name pattern", skriv: `master`

### Steg 3: Konfigurera Protection Rules

#### ✅ Require a pull request before merging

**Aktivera:** ✅ JA

**Settings:**
- ✅ **Require approvals**: 0 (för solo-utveckling) eller 1 (om team)
- ✅ **Dismiss stale pull request approvals when new commits are pushed**
- ✅ **Require review from Code Owners** (skippa om du inte har CODEOWNERS-fil)

**Varför?**
- All kod måste gå via PR (inga direkta commits till master)
- Self-review innan merge (även för solo-utvecklare)
- Tydlig historik av vad som mergats

#### ✅ Require status checks to pass before merging

**Aktivera:** ✅ JA

**Settings:**
- ✅ **Require branches to be up to date before merging**

**Status checks att kräva:**
Efter första PR:n kommer dessa status checks att synas:
- ✅ `code-quality` (Code Quality Checks job)
- ✅ `build` (Build and Test job)

**Varför?**
- CI/CD måste vara grön innan merge
- Inga tester får faila
- Kodformateringen måste vara korrekt

#### ✅ Require conversation resolution before merging

**Aktivera:** ✅ JA (rekommenderat)

**Varför?**
- Alla kommentarer i PR måste resolvas
- Säkerställer att feedback tas om hand

#### ✅ Require signed commits

**Aktivera:** ⚠️ OPTIONAL (rekommenderas för säkerhet)

**Setup för signerade commits:**
```bash
# Generera GPG key
gpg --full-generate-key

# Lista keys
gpg --list-secret-keys --keyid-format=long

# Exportera public key
gpg --armor --export YOUR_KEY_ID

# Lägg till i GitHub Settings → SSH and GPG keys

# Konfigurera Git
git config --global user.signingkey YOUR_KEY_ID
git config --global commit.gpgsign true
```

#### ✅ Require linear history

**Aktivera:** ⚠️ OPTIONAL (rekommenderas)

**Effekt:**
- Kräver att merges görs med "Squash and merge" eller "Rebase and merge"
- Ingen merge commits tillåts
- Ger en ren, linjär historik

**Varför?**
- Enklare att följa historiken
- `git log` blir mycket lättare att läsa
- Varje merge = en commit på master

#### ⚠️ Require deployments to succeed before merging

**Aktivera:** ❌ NEJ (inte relevant för detta projekt ännu)

#### ✅ Lock branch

**Aktivera:** ❌ NEJ

**Varför?**
- Skulle låsa branchen helt (ingen kan pusha alls)
- Bara användbart för arkiverade branches

#### ✅ Do not allow bypassing the above settings

**Aktivera:** ✅ JA (rekommenderas starkt)

**Settings:**
- ❌ **Allow specified actors to bypass required pull requests** - LÅT VARA TOM
  (Även admins måste följa reglerna)

#### ✅ Restrict who can push to matching branches

**Aktivera:** ⚠️ OPTIONAL

**När att använda?**
- I team där bara vissa ska kunna merge
- För open source projekt med maintainers
- Inte nödvändigt för solo-projekt

#### ✅ Allow force pushes

**Aktivera:** ❌ NEJ (viktig säkerhetsregel!)

**Varför?**
- Force push kan förstöra historik
- Risk för dataförlust
- Aldrig tillåt på master

#### ✅ Allow deletions

**Aktivera:** ❌ NEJ (viktig säkerhetsregel!)

**Varför?**
- Förhindrar oavsiktlig deletion av master branch
- Extra säkerhet

### Steg 4: Spara Rules

1. Scrolla ner till botten
2. Klicka på **Create** eller **Save changes**

---

## ✅ Rekommenderad Konfiguration Sammanfattning

### För Solo-Utvecklare (DIN SITUATION):

```
✅ Require a pull request before merging
   └─ Require approvals: 0
   └─ Dismiss stale pull request approvals: ✅

✅ Require status checks to pass before merging
   └─ Require branches to be up to date: ✅
   └─ Status checks: code-quality, build

✅ Require conversation resolution: ✅

⚠️ Require signed commits: OPTIONAL

✅ Require linear history: ✅ (rekommenderas)

✅ Do not allow bypassing: ✅

❌ Allow force pushes: NEJ
❌ Allow deletions: NEJ
```

### För Team (2+ Utvecklare):

```
✅ Require a pull request before merging
   └─ Require approvals: 1 eller 2
   └─ Dismiss stale pull request approvals: ✅
   └─ Require review from Code Owners: ✅

✅ Require status checks to pass before merging
   └─ Require branches to be up to date: ✅
   └─ Status checks: code-quality, build

✅ Require conversation resolution: ✅

✅ Require signed commits: ✅ (starkt rekommenderat)

✅ Require linear history: ✅

✅ Do not allow bypassing: ✅

❌ Allow force pushes: NEJ
❌ Allow deletions: NEJ
```

---

## 🧪 Testa Branch Protection

Efter att ha aktiverat branch protection:

### Test 1: Försök pusha direkt till master

```bash
git checkout master
echo "test" > test.txt
git add test.txt
git commit -m "test: direct push"
git push origin master
```

**Förväntat resultat:**
```
remote: error: GH006: Protected branch update failed for refs/heads/master.
remote: error: Changes must be made through a pull request.
```

✅ **SUCCESS!** Branch protection fungerar.

### Test 2: Skapa PR med failing tests

```bash
git checkout -b test/failing-test
# Ändra något som gör att tester failar
git push origin test/failing-test
# Skapa PR via GitHub UI
```

**Förväntat resultat:**
- PR skapas
- CI/CD körs
- Status check failar
- **Merge button är disabled**
- Meddelande: "Merging is blocked - Required status checks must pass"

✅ **SUCCESS!** CI/CD integration fungerar.

### Test 3: Skapa valid PR

```bash
git checkout -b feature/valid-feature
# Gör ändring
git commit -m "feat: add valid feature"
git push origin feature/valid-feature
# Skapa PR via GitHub UI
```

**Förväntat resultat:**
- PR skapas
- CI/CD körs och blir grön ✅
- **Merge button är enabled**
- "Squash and merge" är default

✅ **SUCCESS!** Happy path fungerar.

---

## 🔄 Workflow Efter Branch Protection

### Tidigare (Direkt till master):

```bash
git add .
git commit -m "feat: new feature"
git push origin master  # ✅ Fungerar
```

### Nu (Via Pull Request):

```bash
# 1. Skapa feature branch
git checkout -b feature/my-feature

# 2. Gör ändringar
git add .
git commit -m "feat: add my feature"

# 3. Pusha branch
git push origin feature/my-feature

# 4. Skapa PR på GitHub
# - Gå till repository
# - Klicka "Compare & pull request"
# - Fyll i PR template
# - Klicka "Create pull request"

# 5. Vänta på CI/CD (automatiskt)

# 6. Merge via GitHub UI
# - Klicka "Squash and merge"
# - Confirm merge

# 7. Cleanup lokalt
git checkout master
git pull origin master
git branch -d feature/my-feature
```

---

## 📚 Additional Resources

- [GitHub Docs: About protected branches](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches)
- [GitHub Docs: Managing a branch protection rule](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/managing-a-branch-protection-rule)
- [Git Flow vs GitHub Flow](https://www.atlassian.com/git/tutorials/comparing-workflows)

---

## ❓ Troubleshooting

### Problem: "Status checks not found"

**Orsak:** Inga PR:s har körts än, så GitHub vet inte vilka status checks som finns.

**Lösning:**
1. Skapa och merga första PR:n utan status check requirement
2. Efter första PR:n, gå tillbaka och lägg till status checks
3. De kommer nu att synas i dropdown:en

### Problem: "Can't push to protected branch"

**Orsak:** Branch protection är aktiverad (fungerar som tänkt!)

**Lösning:**
1. Skapa feature branch
2. Pusha till feature branch
3. Skapa PR
4. Merge via GitHub UI

### Problem: CI/CD failar men jag vet att koden är OK

**Orsak:** Kan vara tillfälligt problem med GitHub Actions

**Lösning:**
1. Gå till "Actions" tab i GitHub
2. Klicka på det failade workflow:t
3. Klicka "Re-run failed jobs"
4. Om det fortsätter faila, kolla loggarna noga

---

**Skapad:** 2025-01-25
**Status:** Ready for Implementation
