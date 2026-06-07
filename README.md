# CDR Photo Match Pro

Windows 7 compatible offline C# WinForms software for finding matching `.cdr` files from a JPG/PNG/BMP photo.

## Important practical note
CorelDRAW `.cdr` is a proprietary format. This app works by trying to read an embedded preview/thumbnail from the CDR file. It also supports sidecar preview images with the same filename, e.g.:

- `Design1.cdr`
- `Design1.jpg` or `Design1.png`

If a CDR file has no embedded preview and no sidecar image, the app cannot visually compare that CDR and will safely skip it.

## Features
- JPG/PNG/BMP upload
- Multiple folder scan
- Subfolder scan
- Match percentage
- Full path display
- Open folder
- Copy path
- Export CSV/TXT
- Background scan
- Safe read-only mode

## Build
Upload this repository to GitHub. GitHub Actions will create a ZIP artifact containing the portable EXE.
