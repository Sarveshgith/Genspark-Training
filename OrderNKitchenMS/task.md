# Tasks for Responsive Navigation & Table Optimizations

- [x] Create Bottom Navigation Component
  - [x] Create `bottom-nav.ts` component controller
  - [x] Create `bottom-nav.html` visual layout template
  - [x] Create `bottom-nav.css` animations and styles
- [x] Refactor Sidebar Component
  - [x] Remove mobile-specific properties from `sidebar.ts`
  - [x] Restructure `sidebar.html` to be desktop-only (`hidden lg:flex`)
- [x] Wire layout structure in root app template
  - [x] Update `app.ts` imports
  - [x] Update `app.html` grid and compensate bottom padding
- [x] Optimize Table Layouts for Mobile
  - [x] Hide optional columns (Chef, Waiter, Time) in Order Registry, fallback to expanded row view
  - [x] Hide optional columns (Unit Cost, Status) in Inventory Manager stock list
- [x] Verification
  - [x] Run production build compile checks
