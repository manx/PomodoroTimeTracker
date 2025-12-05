# Testing Checklist - Pomodoro Time Tracker

## Pomodoro Start
- [ ] Client dropdown populated from database
- [ ] Last selected client pre-selected
- [ ] Project dropdown filtered by selected client
- [ ] Last selected project pre-selected
- [ ] Objective field required (90 chars max)
- [ ] Start button disabled without objective
- [ ] Duration editable before start

## Pomodoro Running
- [ ] Timer counts down correctly (1 second intervals)
- [ ] Progress ring updates smoothly
- [ ] Pause button works immediately
- [ ] Resume restores timer exactly
- [ ] Stop shows confirmation dialog
- [ ] Resume from dialog continues timer
- [ ] Save from dialog creates partial session with note
- [ ] Discard from dialog deletes session from database

## Wrap Up Period
- [ ] Wrap up notification plays when work period ends
- [ ] Timer transitions to WrapUp state
- [ ] InfoBar message displays correctly
- [ ] Progress ring resets and counts down wrap up period
- [ ] Pause/Resume/Stop buttons remain active
- [ ] Main alarm plays when wrap up period expires
- [ ] Break starts automatically after wrap up

## Break Cycle
- [ ] Short break after pomodoros 1-3
- [ ] Long break after pomodoro 4
- [ ] Cycle resets to 0 after long break
- [ ] No pause/stop buttons during breaks
- [ ] Session type label shows correct break type
- [ ] Break duration matches settings

## Timer Window
- [ ] Window stays on top of other windows
- [ ] Draggable via entire surface
- [ ] Resizable from edges and corners
- [ ] Progress bar fills correctly
- [ ] Right-click menu appears
- [ ] Context menu commands work
- [ ] Add time feature works (+1, +2, +5 min)

## Settings
- [ ] All settings save to database
- [ ] Auto-calculate feature works correctly
- [ ] Defaults restore properly
- [ ] Settings persist between app sessions
- [ ] Changes immediately affect new sessions
- [ ] Sound test buttons work

## Client & Project Management
- [ ] CRUD operations work for clients
- [ ] CRUD operations work for projects
- [ ] Client filter works in project list
- [ ] Deleting client sets project ClientId to NULL
- [ ] Navigation between pages works
- [ ] Data persists correctly

## Report View
- [ ] Period selection works (Daily/Weekly/Monthly/Custom)
- [ ] Date navigation works
- [ ] Client/Project filters work
- [ ] Summary cards display correct totals
- [ ] Project breakdown shows correct data
