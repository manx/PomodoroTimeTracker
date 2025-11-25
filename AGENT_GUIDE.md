# Agent Guide - Claude Code

En guide för att använda befintliga agenter i Claude Code för Pomodoro Time Tracker-projektet.

## Vad är agenter?

Agenter är specialiserade subprocess som hanterar komplexa uppgifter autonomt. De har tillgång till olika verktyg och kan utföra flerstegsuppgifter utan kontinuerlig interaktion.

## Tillgängliga agenttyper

### 1. Explore Agent
**Användningsområde**: Snabb utforskning av kodbas

**När ska du använda den?**
- Hitta filer med mönster: `src/components/**/*.tsx`
- Söka nyckelord i kod: `IPomodoroSessionService`
- Förstå hur saker fungerar: "Hur fungerar timer-logiken?"

**Noggrannhetsnivåer**:
- `quick` - Grundläggande sökningar
- `medium` - Moderat utforskning
- `very thorough` - Omfattande analys

**Exempel**:
```
"Utforska hur session-hantering fungerar i projektet"
"Hitta alla XAML-filer som använder PomodoroViewModel"
"Var implementeras wrap-up period funktionaliteten?"
```

### 2. Plan Agent
**Användningsområde**: Planera implementationer innan du börjar koda

**När ska du använda den?**
- Stora ändringar som påverkar många filer
- Arkitektoniska beslut (WebSockets vs polling, Redux vs Context)
- Oklara krav som behöver utforskas först
- Flera möjliga tillvägagångssätt med olika avvägningar

**Exempel**:
```
"Planera implementering av ljudnotifikationer med volymkontroll"
"Designa ett system för att exportera sessions-data till CSV/JSON"
"Planera migration från SQLite till SQL Server"
```

### 3. General-Purpose Agent
**Användningsområde**: Komplexa flerstegsuppgifter

**När ska du använda den?**
- Sökningar som kräver flera försök
- Uppgifter med många beroenden
- Komplexa research-uppgifter

**Exempel**:
```
"Hitta och fixa alla TODO-kommentarer relaterade till ljudimplementering"
"Undersök alla platser där timer state ändras och dokumentera flödet"
```

### 4. Claude-Code-Guide Agent
**Användningsområde**: Frågor om Claude Code själv

**När ska du använda den?**
- "Kan Claude Code...?"
- "Hur använder jag hooks i Claude Code?"
- "Hur skriver jag en slash command?"
- Frågor om Claude Agent SDK

**Exempel**:
```
"Hur skapar jag en custom slash command?"
"Vad är skillnaden mellan hooks och skills?"
"Hur installerar jag en MCP server?"
```

## Hur anropar du agenter?

### Metod 1: Be Claude direkt
Du behöver inte anropa agenter manuellt. Be bara Claude utföra uppgiften:

```
"Utforska hur timer-fönstret implementeras"
"Planera hur vi ska lägga till ljudnotifikationer"
```

Claude väljer automatiskt rätt agent baserat på uppgiften.

### Metod 2: Explicit begäran
Om du vill vara specifik:

```
"Använd Explore agent för att hitta alla ViewModels"
"Kör en Plan agent för att designa dashboard-vyn"
```

## Projekt-specifika användningsfall

### För Pomodoro Time Tracker

**Utforska befintlig kod**:
- "Utforska hur PomodoroViewModel hanterar timer states"
- "Hitta alla platser där wrap-up period används"
- "Var implementeras session save/discard logiken?"

**Planera nya funktioner**:
- "Planera implementering av toast notifications"
- "Designa export-funktionalitet för sessions"
- "Planera Dashboard-vyn med statistik"

**Hitta och fixa**:
- "Hitta alla TODO-kommentarer i projektet"
- "Hitta alla platser som behöver ljudimplementering"
- "Undersök var error handling saknas"

**Förstå arkitektur**:
- "Förklara dataflödet från ViewModel till Database"
- "Hur fungerar dependency injection i projektet?"
- "Vilka patterns används för MVVM?"

## Best practices

### När INTE använda agenter

Använd INTE agenter för:
- Läsa en specifik fil (använd Read tool direkt)
- Söka efter specifik klass: `class Foo` (använd Glob direkt)
- Söka i 2-3 specifika filer (använd Read direkt)
- Enkla straightforward uppgifter

### När ALLTID använda agenter

Använd agenter för:
- Öppen utforskning: "Hur fungerar X?"
- Sökningar som kan kräva flera försök
- Komplexa flerstegsuppgifter
- Planering av stora ändringar

### Parallella agenter

Du kan köra flera agenter samtidigt:

```
"Kör Explore agent för att hitta timer-logik OCH
Plan agent för att designa ljudsystemet - parallellt"
```

## Tips för effektiv agentanvändning

1. **Var specifik med scope**:
   - Bra: "Utforska timer window implementation i TimerWindow.xaml.cs"
   - Dåligt: "Utforska projektet"

2. **Ange noggrannhetsnivå för Explore**:
   - "Quick search för ViewModel-filer"
   - "Very thorough analys av session persistence"

3. **Låt agenten arbeta**:
   - Agenter är autonoma - de returnerar resultat när de är klara
   - Du får ett meddelande tillbaka med resultatet

4. **Återanvänd agenter**:
   - Claude kan återanvända en befintlig agent istället för att starta ny
   - Mer effektivt och behåller kontext

## Vanliga scenarios

### Scenario 1: Förstå befintlig feature
```
"Utforska hur wrap-up period funktionen implementerades.
Hitta alla relaterade filer och förklara flödet."
```

### Scenario 2: Planera ny feature
```
"Jag vill lägga till ljudnotifikationer.
Planera implementeringen - vilka filer behöver ändras,
vilka NuGet packages behövs, och hur integrerar vi med settings?"
```

### Scenario 3: Hitta alla instanser
```
"Hitta alla TODO-kommentarer i projektet och
kategorisera dem efter prioritet (HIGH/MEDIUM/LOW)"
```

### Scenario 4: Debugging
```
"Timer window visar vit bar överst.
Utforska hur borderless window implementerades och
identifiera potentiella orsaker till problemet."
```

## Felsökning

**Problem**: Agent hittar inte vad jag söker
**Lösning**: Var mer specifik med sökord eller filnamn

**Problem**: Agent tar för lång tid
**Lösning**: Begränsa scope, använd "quick" istället för "very thorough"

**Problem**: Resultat är för omfattande
**Lösning**: Be om sammanfattning eller specifika delar

## Skills - Automatisk Expertis

**Vad är skills?**
Skills är modulära dokumentationspaket som aktiveras automatiskt baserat på kontext. Till skillnad från agenter (som du anropar manuellt), laddar Claude skills automatiskt när uppgiften matchar skill-kontexten.

### Skillnad: Agenter vs Skills

| | **Agenter** | **Skills** |
|---|---|---|
| **Aktivering** | Du ber Claude använda dem | Automatisk baserat på kontext |
| **Syfte** | Utföra uppgifter | Tillhandahålla expertis |
| **Användning** | "Utforska X", "Planera Y" | Laddas när du säger "tester", "review", etc. |

### Installerade Skills

#### 1. Unit Test Specialist
**Aktiveras när:** Du nämner "tester", "unit tests", "test coverage"

**Innehåller:**
- xUnit patterns för .NET 9
- Moq mocking patterns
- FluentAssertions examples
- ViewModel testing med CommunityToolkit.Mvvm
- EF Core InMemory testing
- Test naming conventions (AAA pattern)

**Exempel:**
```
"Jag behöver tester för PomodoroViewModel"
→ Skill aktiveras automatiskt
→ Claude använder projektspecifika patterns
```

**Plats:** `.claude/skills/unit-test-specialist/SKILL.md`

### Skapa egna Skills

För att skapa en skill:

1. **Skapa mapp:** `.claude/skills/my-skill/`
2. **Lägg till SKILL.md:** Beskriv när skill ska aktiveras och vilken expertis den ger
3. **Valfritt script:** Lägg till shell scripts för procedurella uppgifter

**Exempel struktur:**
```
.claude/skills/
├── unit-test-specialist/
│   └── SKILL.md
├── ef-migrations/
│   ├── SKILL.md
│   └── migrate.sh
└── winui-patterns/
    └── SKILL.md
```

### Förslag på fler Skills för projektet

- **Code Review Specialist** - SOLID principles, MVVM patterns, security checks
- **EF Core Migration Expert** - Migration best practices, rollback strategies
- **WinUI 3 Patterns** - XAML patterns, data binding, styling guidelines
- **Performance Optimizer** - Profiling, async/await patterns, memory optimization

## Nästa steg

Efter att du behärskar befintliga agenter och skills kan du:
1. Skapa custom slash commands som använder agenter
2. Sätta upp hooks för automatiska agentanrop
3. Skapa fler projektspecifika skills
4. Integrera agenter och skills i ditt development workflow

---

**Senast uppdaterad**: 2025-01-25
**Relaterade filer**: CLAUDE.md, README.md, .claude/skills/unit-test-specialist/SKILL.md
