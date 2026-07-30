# Releasing

> **Maintainer documentation.** If you consume this package, you do not need any of this —
> see [CONTRIBUTING.md](../CONTRIBUTING.md) for how to request a change.

The git tag is the version. Tag `v1.2.3` publishes `1.2.3`. No file in the repo records the version.

Release notes come from the annotated tag message. There is no `CHANGELOG.md` — the
[Releases page](https://github.com/dennisrahmen/DR.Simple_UI/releases) is the changelog.

## Cutting a release

1. Decide the version using the rules below.
2. Write the notes to a file outside the repo.
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

## Version rules

Judged by what a consuming app sees. Take the highest applicable level.

**Major** — an app breaks or changes appearance without editing anything:

- renaming or removing a token, class or modifier
- changing a frame component's markup, parameters or emitted classes
- renaming a shipped asset path or the JS global
- changing an existing rule's values enough to move layout or colour
- a change that makes an existing app override stop working

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

1. nuget.org → your username → **Trusted Publishing** → add a policy:

   | Field | Value |
   |---|---|
   | Repository Owner | `dennisrahmen` |
   | Repository | `DR.Simple_UI` |
   | Workflow File | `release.yml` |
   | Environment | *(empty)* |

2. Add a `NUGET_USER` repository secret containing your nuget.org profile name, not an email address.

### Constraints

- The policy matches on the workflow **file name**. Renaming `release.yml` breaks publishing until the
  policy is updated to match.
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
