# DocFX Documentation

## Prerequisites
Install DocFX (global tool or download):

```
dotnet tool update -g docfx
```

## Generate Metadata, Site, and PDF
From the `docs` folder:

```
# Generate API metadata
docfx metadata

# Build static site into _site
docfx build

# Build PDF (outputs to docs/pdf)
docfx pdf
```

Open `_site/index.html` in a browser. PDF will appear under `pdf/` (name derives from _appTitle or docfx defaults).

### Word (DOCX) Option
DocFX does not natively emit DOCX. Common approaches:
1. Convert the generated PDF to DOCX (e.g. LibreOffice, pandoc).
2. Use pandoc directly on the Markdown sources:
	```
	pandoc -s articles/intro.md -o output.docx
	```
	For a larger set, concatenate or provide a manifest file.

## Structure
- `docfx.json` configuration
- `articles/` conceptual guides
- `api/` generated YAML + API landing page

## Next Steps
- Add more guides under `articles/`
- Add logo or favicon
- Configure CI to run `docfx build` and publish `_site` (e.g. GitHub Pages)
