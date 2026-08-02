# Releasing

> **Maintainer documentation.** If you consume this package, you do not need any of this —
> see [CONTRIBUTING.md](../CONTRIBUTING.md) for how to request a change.

The git tag is the version. Tag `v1.2.3` publishes `1.2.3`. No file in the repo records the version.

Release notes come from the annotated tag message. There is no `CHANGELOG.md` — the
[Releases page](https://github.com/dennisrahmen/DR.Simple_UI/releases) is the changelog.

## Two packages, two tag prefixes

| Package | Tag | Workflow | Contains |
|---|---|---|---|
| `DR.Simple_UI` | `v1.2.3` | `release.yml` | the library |
| `DR.Simple_UI.Templates` | `templates-v1.2.3` | `release-template.yml` | `dotnet new dr-blazor` |

They are versioned independently: a template's version tracks the shape of the app it generates, not the
library's contract, and an app generated today keeps working when the library bumps. `v*` does not match
`templates-v*`, so neither tag fires the other's workflow.

The template release runs the full test suite and `build/verify-template.sh`, which generates a project
and compiles it with `-warnaserror`. It packs the library too, but only to generate against — the push
step names the template package explicitly, because a library nupkg built there carries the csproj's
floor version and pushing it would permanently burn a version number.

**Order matters when the template needs a library version that is not out yet.** The generated app
references `DrSimpleUiVersion` from `template.json`, so release the library first; a template published
ahead of it generates an app that cannot restore. `The_project_template_references_a_version_that_has_the_components`
only checks the floor of `0.2.0`, not that the version exists on nuget.org.

## Cutting a release

1. Decide the version using the rules below.
2. Write the notes to a file outside the repo. List every CSS class the release adds: a consuming app
   that already styles one of those names sees its appearance change on upgrade with no error, so the
   list is what lets it grep first.
3. Tag and push:

   ```bash
   git tag -a v0.2.0 -F /tmp/notes.md
   git push origin v0.2.0
   ```

`release.yml` builds, tests, packs, verifies the package contents, publishes to nuget.org, and creates the
GitHub release. The release body is the tag message plus an install snippet and a compare link to the
previous tag.

The first line of the tag message becomes the release title suffix; the rest becomes the body. A
lightweight tag (`git tag v0.2.0`) has no message, and the release falls back to the commit list.

A published nuget.org version cannot be replaced, reused or withdrawn.

## Before 1.0.0

**While the major version is 0, breaking changes go out in a MINOR bump.** That is what SemVer says
`0.x` means, and it is the whole point of not having reached 1.0.0 yet: the design is still being got
right, and a rename that makes the library better is worth more than a stable name that is wrong.

So during `0.x`:

- Classify the change with the rules below anyway — the classification is what the release notes have
  to state.
- A change classified **Major** ships as the next **minor** (`0.2.0` → `0.3.0`), with the breaks
  listed at the top of the notes.
- Do **not** avoid a breaking change to keep a version number small, and do not leave a compatibility
  shim behind to soften one. A fallback kept for an old caller is a second code path nobody tests, and
  it outlives the migration it was for. Change it properly and say so.
- Do **not** ship the same idea twice under two names because renaming the first would break someone.
  `.user-menu-*` was removed rather than left beside `.menu-*` for exactly this reason.

From 1.0.0 the levels below mean what they say, and a Major becomes a real major bump.

## Version rules

Judged by what a consuming app sees. Take the highest applicable level.

**Major** — an app breaks or changes appearance without editing anything:

- renaming or removing a token, class or modifier
- changing a frame component's markup, parameters or emitted classes
- renaming a shipped asset path or the JS global
- changing an existing rule's values enough to move layout or colour
- a change that makes an existing app override stop working
- **the inverse: a change that makes an app override start winning where it used to lose.** Easy to
  miss, because nothing in this repo breaks and the app's own CSS is what changes appearance. Two ways
  it happens: lowering a library rule's specificity, and moving a rule into a cascade layer — an
  unlayered app rule beats every layered one. Compact density is the worked example in
  [`architecture.md`](architecture.md#three-consequences-worth-knowing-before-upgrading).
- moving a part into a different cascade layer, or reordering the layers

**Minor** — additive and backwards compatible:

- a new token, class, variant, component or catalogue page
- a new optional parameter or JS function

**Patch** — no contract change:

- correcting a wrong value
- docs, tests, CI

When a change is arguable, use the higher level.

## Trusted publishing

The release job exchanges its GitHub OIDC token for a single-use NuGet API key valid for one hour. No
long-lived API key is stored in the repository.

### Setup

1. nuget.org → your username → **Trusted Publishing** → add a policy **per workflow file**, because the
   policy matches on the file name:

   | Field | Library | Template |
   |---|---|---|
   | Repository Owner | `dennisrahmen` | `dennisrahmen` |
   | Repository | `DR.Simple_UI` | `DR.Simple_UI` |
   | Workflow File | `release.yml` | `release-template.yml` |
   | Environment | *(empty)* | *(empty)* |

   The template's policy must also permit a package ID that does not exist yet — reserve
   `DR.Simple_UI.Templates` or allow the `DR.Simple_UI.*` prefix. A policy naming only an existing
   package ID cannot create one, so the first template publish fails without this.

2. Add a `NUGET_USER` repository secret containing your nuget.org profile name, not an email address.
   Both workflows read the same secret.

### Constraints

- The policy matches on the workflow **file name**. Renaming `release.yml` or `release-template.yml`
  breaks that package's publishing until the policy is updated to match.
- The API key is requested immediately before the push. It expires an hour after issue.
- A policy on a private repository is temporarily active for 7 days and goes inactive if no publish
  happens in that window. Public repositories are unaffected.
- If **Trusted Publishing** is not in your nuget.org account menu, it is not yet enabled for your account.

## GitHub Pages

`pages.yml` publishes the catalogue to <https://github.dennisrahmen.de/> on pushes to `main` that touch
`wwwroot/`.

The custom domain requires a DNS record:

| Type | Name | Target |
|---|---|---|
| `CNAME` | `github` | `dennisrahmen.github.io` |

The domain is set both in the repository's Pages settings and as a `CNAME` file in the deployed artifact.
Both are required — without the file, a deploy clears the custom domain.
