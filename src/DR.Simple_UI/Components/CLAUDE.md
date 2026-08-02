# `Components/` — tier 1 only

**Tier 1 is the frame: shell, sidebar, nav, header, user widget.** Nothing else belongs here.

Before adding a component, read the two-tier rule in the repo root `CLAUDE.md`. The short version:

> **New content UI is a CSS class, not a component.** Do not add a `<DataTable>`, a `<Card>`, a
> `<Badge>` or any other wrapper around page content. If anyone needs to adjust the inside of it, it
> is a class.

Which tier a thing belongs to is decided by that one question, not by how convenient a component
would be.

## The emitted markup is a version contract

Four apps depend on these classes and this nesting. Changing either restyles all four with no app
edit and no error, so per the release rules it is **Major**:

- renaming or removing an emitted class
- changing the nesting order or the element type
- renaming or removing a parameter, or changing its type
- making an optional parameter required

Adding a component or an optional parameter is **minor**.

The `Components/` test files assert the emitted markup for exactly this reason. If a change makes one of
those tests fail, the test is the specification — the failure is telling you the version has to go
up, not that the assertion is stale.

## Conventions every component follows

1. **Markup in the `.razor`, API in the `.razor.cs`.** The partial class carries the XML docs for the
   type and every parameter. This is not cosmetic: `GenerateDocumentationFile` plus
   `TreatWarningsAsErrors` makes an undocumented public member a build error, and the class generated
   from a `.razor` file has nowhere to put a doc comment.
2. **`@attributes` first, the computed `class` last.** Everything else is splatted so an app can add
   `id`, `data-*` and ARIA.
3. **Declare a `Class` parameter.** Blazor matches parameters case-insensitively, so `Class` also
   captures a plain `class="…"` written at the call site — which turns the destructive spelling into
   the additive one. Without it, `<Sidebar class="x">` would land in `AdditionalAttributes` and wipe
   `.sidebar`, breaking the layout with no error. There is a test per component for this.
4. **Emit only classes the stylesheet defines.** `Components/ClassContractTests` renders every
   component with every feature on and fails on a class that exists nowhere in
   `wwwroot/css/DR.Simple_UI.css`, and again on one that `catalogue/frame.html` never shows. A
   component and the hand-written markup in the catalogue must describe the same frame.
5. **No styling in the component.** No inline `style`, no `.razor.css`. Scoped CSS rewrites selectors
   to add a `b-{hash}` attribute, which would make the rule unreachable from the app's own overrides
   and split the frame's appearance across two files.
6. **Do not require JavaScript.** The frame must render and be usable with scripting blocked. The
   user widget's dropdown is opened by Blazor state and dismissed by a scrim element and an
   `@onkeydown`, not by `DR.Simple_UI.js`.
7. **A control that does nothing is not a control.** `UserWidget` renders its trigger as a `<div>`
   when there is no menu to open, and as a `<button>` only when there is — a button that does nothing
   is still announced and still reached by keyboard.
8. **Prefer a native element over ARIA.** The dropdown is a disclosure with `aria-expanded`, not
   `role="menu"`: declaring `role="menu"` promises arrow-key navigation and a roving tabindex, and
   promising that without implementing it is worse than promising nothing.

## Two Razor traps the examples must not fall into

Both were found by generating a project from a template and compiling it. **There is no template any
more, so nothing compiles a documented example** — the guard named below is a source scan, which catches
these two shapes but cannot catch a third Razor rule nobody has hit yet. Compile a documented snippet by
hand when changing one.

1. **A component with any named `RenderFragment` stops accepting loose child content.** `AppShell` has
   `Navigation` and `Header`, `Sidebar` has `Tools`, `AppHeader` has `Start` — so every example must
   spell out `<ChildContent>` around the default content, or the app fails to build with `RZ9996`.
   This is Razor's rule, not a choice made here, and it cannot be designed away while the named slots
   exist.
2. **Text containing `@` cannot go straight into an attribute.** `Secondary="a@b.com"` is parsed as a
   C# expression at the `@` and fails with `RZ9986`. Bind it to a field — which is what a real app
   does anyway, since the value comes from the authentication state.

`Documented_component_examples_avoid_the_two_Razor_traps` scans every documented example for both.

## Naming

- **`NavItem`, never `NavLink`.** `Microsoft.AspNetCore.Components.Routing.NavLink` is in scope in
  every Blazor app through its own `_Imports.razor`. A component called `NavLink` here would become
  an ambiguous reference the moment an app added `@using DR.Simple_UI.Components`, and would break
  every existing `<NavLink>` in that app at the same time.
- Check any new component name the same way: assume the app has `@using DR.Simple_UI.Components` and
  ask what it now collides with. `Layout`, `Router`, `PageTitle`, `HeadContent`, `ErrorBoundary`,
  `SectionOutlet`, `Virtualize` and `InputText` are all taken by the framework.
- Class names emitted by a component follow the CSS naming rules in `css-parts/CLAUDE.md`.

## Adding a component

1. Write `Name.razor` and `Name.razor.cs`, following the conventions above.
2. Add a section to `wwwroot/catalogue/frame.html` showing the markup it emits — the class-contract
   test fails otherwise, and a reader writing the markup by hand has nothing to copy.
3. Add a test file under `src/DR.Simple_UI.Tests/Components/`, one per component, covering the emitted
   classes, the nesting, and every parameter that changes the markup.
4. Document it in `docs/architecture.md` and in the components list in the root `CLAUDE.md`.
5. `dotnet test`.
