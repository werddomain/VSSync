# Publishing VS²Sync Extensions

This guide explains how to publish VS²Sync extensions to the VS Code Marketplace and Visual Studio Marketplace.

## Table of Contents

- [Version Tagging](#version-tagging)
- [Auto-Increment Versions](#auto-increment-versions)
- [Setting Up Marketplace Publishing](#setting-up-marketplace-publishing)
- [Triggering a Release](#triggering-a-release)
- [Manual Publishing](#manual-publishing)

## Version Tagging

VS²Sync uses Git tags to manage versions. When you push a version tag (without ending with 'b'), the GitHub Action automatically creates a release and optionally publishes to the marketplaces.

### Tag Format

- **Release tags**: `v1.0.0`, `v1.2.3`, `v2.0.0` (triggers release + optional marketplace publish)
- **Beta tags**: `v1.0.0b`, `v1.2.3b1`, `v2.0.0-beta` (does NOT trigger release)

### Creating a Version Tag

#### Option 1: Tag the current commit

```bash
# Create a tag for the current commit
git tag v1.0.0

# Push the tag to GitHub (triggers release workflow)
git push origin v1.0.0
```

#### Option 2: Tag a specific commit

```bash
# Create a tag for a specific commit SHA
git tag v1.0.0 abc1234

# Push the tag to GitHub
git push origin v1.0.0
```

#### Option 3: Tag a branch's HEAD

```bash
# Create a tag at the tip of main branch
git tag v1.0.0 main

# Push the tag to GitHub
git push origin v1.0.0
```

### Annotated Tags (Recommended)

For releases, use annotated tags to include additional metadata:

```bash
# Create an annotated tag with a message
git tag -a v1.0.0 -m "Release version 1.0.0 - Initial stable release"

# Push the tag
git push origin v1.0.0
```

### Listing Tags

```bash
# List all tags
git tag

# List tags matching a pattern
git tag -l "v1.*"

# Show tag details
git show v1.0.0
```

### Deleting Tags

```bash
# Delete a local tag
git tag -d v1.0.0

# Delete a remote tag
git push origin --delete v1.0.0
```

## Auto-Increment Versions

The release workflow automatically updates version numbers in the following files based on the Git tag:

- **VS Code Extension**: `vscode-extension/package.json` → `version` field
- **Visual Studio Extension**: `visual-studio-extension/VSSync/source.extension.vsixmanifest` → `Identity Version` attribute

### Semantic Versioning

Follow [Semantic Versioning](https://semver.org/) (SemVer) for version numbers:

- **MAJOR** (`X.0.0`): Breaking changes, incompatible API changes
- **MINOR** (`0.X.0`): New features, backward-compatible
- **PATCH** (`0.0.X`): Bug fixes, backward-compatible

### Version Increment Examples

| Current | Change Type | New Version |
|---------|-------------|-------------|
| 1.0.0   | Bug fix     | 1.0.1       |
| 1.0.0   | New feature | 1.1.0       |
| 1.0.0   | Breaking    | 2.0.0       |

### Pre-Release Versions (Beta)

For pre-release versions that should NOT trigger marketplace publishing:

```bash
# Beta version - will NOT trigger release workflow
git tag v1.1.0b
git push origin v1.1.0b
```

## Setting Up Marketplace Publishing

### Prerequisites

To enable automated publishing to marketplaces, you need to configure repository secrets and variables.

### VS Code Marketplace Setup

1. **Create a Publisher Account**
   - Go to [Visual Studio Marketplace Publisher Management](https://marketplace.visualstudio.com/manage)
   - Sign in with your Microsoft account
   - Create a publisher if you don't have one

2. **Generate a Personal Access Token (PAT)**
   - Go to [Azure DevOps](https://dev.azure.com/)
   - Click on User Settings (gear icon) → Personal Access Tokens
   - Click "New Token"
   - Set:
     - Name: `VSCE Publishing`
     - Organization: `All accessible organizations`
     - Expiration: Choose appropriate duration
     - Scopes: Select "Custom defined" and check:
       - Marketplace → Manage
   - Copy the generated token

3. **Configure GitHub Secrets**
   - Go to your GitHub repository → Settings → Secrets and variables → Actions
   - Add a new repository secret:
     - Name: `VSCE_PAT`
     - Value: Your Personal Access Token from step 2

4. **Enable Publishing**
   - Go to Settings → Secrets and variables → Actions → Variables
   - Add a new repository variable:
     - Name: `PUBLISH_VSCODE_MARKETPLACE`
     - Value: `true`

### Visual Studio Marketplace Setup

1. **Create a Publisher Account**
   - Go to [Visual Studio Marketplace Publisher Management](https://marketplace.visualstudio.com/manage/publishers)
   - Sign in with your Microsoft account
   - Create a publisher if you don't have one

2. **Generate a Personal Access Token (PAT)**
   - Same process as VS Code Marketplace (they use the same Azure DevOps PAT system)
   - Required scopes for VS extensions:
     - Marketplace → Manage

3. **Create a Publish Manifest**
   Create a file `visual-studio-extension/VSSync/publish-manifest.json`:
   ```json
   {
     "$schema": "http://json.schemastore.org/vsix-publish",
     "categories": ["Tools"],
     "identity": {
       "internalName": "VS2Sync"
     },
     "overview": "../../README.md",
     "priceCategory": "free",
     "publisher": "VS-2-Sync",
     "repo": "https://github.com/werddomain/VSSync"
   }
   ```

   > **Note:** The `publisher` value must match your Visual Studio Marketplace publisher ID and the `Publisher` attribute in your `source.extension.vsixmanifest` file.

4. **Configure GitHub Secrets**
   - Add a new repository secret:
     - Name: `VS_MARKETPLACE_PAT`
     - Value: Your Personal Access Token

5. **Enable Publishing**
   - Add a new repository variable:
     - Name: `PUBLISH_VS_MARKETPLACE`
     - Value: `true`

## Triggering a Release

### Release Checklist

Before creating a release tag:

1. **Ensure all tests pass** on the main branch
2. **Update documentation** if needed
3. **Review changelog** or prepare release notes
4. **Verify version numbers** are consistent (or rely on auto-increment)

### Creating a Release

```bash
# 1. Ensure you're on the latest main branch
git checkout main
git pull origin main

# 2. Create an annotated tag
git tag -a v1.0.0 -m "Release v1.0.0 - Description of changes"

# 3. Push the tag (this triggers the release workflow)
git push origin v1.0.0
```

### What Happens After Pushing a Tag

1. **GitHub Actions Workflow Triggers**
   - Extracts version from the tag
   - Builds VS Code extension (Linux runner)
   - Builds Visual Studio extension (Windows runner)

2. **Artifacts Created**
   - `vs-2-sync-{version}.vsix` - VS Code extension
   - `VS-2-Sync-VisualStudio-{version}.vsix` - Visual Studio extension

3. **GitHub Release Created**
   - Release named "Release v{version}"
   - Both VSIX files attached as assets
   - Auto-generated release notes from commits

4. **Marketplace Publishing** (if enabled)
   - VS Code extension published to VS Code Marketplace
   - Visual Studio extension published to Visual Studio Marketplace

### Monitoring the Release

1. Go to GitHub repository → Actions tab
2. Find the "Release Extensions" workflow run
3. Monitor progress and check for any errors

## Manual Publishing

If automated publishing fails or you prefer manual control:

### VS Code Extension

```bash
cd vscode-extension

# Install dependencies
npm ci

# Package the extension
npm install -g @vscode/vsce
vsce package

# Publish to marketplace (requires PAT)
vsce publish -p <YOUR_PAT>
```

### Visual Studio Extension

1. Build the extension in Visual Studio 2022 (Release configuration)
2. Locate the VSIX in `visual-studio-extension/VSSync/bin/Release/`
3. Go to [Visual Studio Marketplace Publisher Management](https://marketplace.visualstudio.com/manage/publishers)
4. Upload the VSIX file manually

## Troubleshooting

### Common Issues

#### "Publisher not found" error
- Verify the `publisher` field in `package.json` matches your marketplace publisher ID
- Ensure your PAT has the correct scopes

#### Build failures
- Check that all dependencies are properly defined
- Verify the correct Node.js version (20.x) is used
- For VS extension, ensure proper .NET SDK and VS SDK are available

#### Release not triggered
- Ensure the tag matches the pattern `v[0-9]+.[0-9]+.[0-9]+`
- Tags ending with 'b' or containing 'beta' won't trigger releases
- Verify the tag was pushed to the remote repository

### Getting Help

If you encounter issues:
1. Check the GitHub Actions logs for detailed error messages
2. Open an issue at [VS²Sync Issues](https://github.com/werddomain/VSSync/issues)

## Quick Reference

| Action | Command |
|--------|---------|
| Create release tag | `git tag -a v1.0.0 -m "Release v1.0.0"` |
| Push tag | `git push origin v1.0.0` |
| Create beta tag | `git tag v1.0.0b` |
| List tags | `git tag -l "v*"` |
| Delete local tag | `git tag -d v1.0.0` |
| Delete remote tag | `git push origin --delete v1.0.0` |
