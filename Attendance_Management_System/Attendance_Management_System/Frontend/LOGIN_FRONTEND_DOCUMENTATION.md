# Login Frontend Update Documentation

Date: 2026-04-02
Branch: JP

## Scope
This update finalizes login-page frontend branding and consistency updates:
- Hero image is now the single source for both login hero and favicon.
- Frontend inline comments were added to explain Razor and CSS syntax blocks.
- Legacy conflicting logo references were removed in prior cleanup.

## Files Updated
1. `Frontend/Views/Shared/_Layout.cshtml`
- Added favicon and apple-touch-icon links in the shared layout head.
- Purpose: apply the Bosconian crest favicon across authenticated pages.

2. `Frontend/Views/Account/Login.cshtml`
- Added favicon and apple-touch-icon links in login head (required because `Layout = null`).
- Added inline Razor comments for:
  - Hero section purpose.
  - Tag Helpers in form/model binding.
  - `site.js` frontend behavior inclusion.

3. `Frontend/wwwroot/css/site.css`
- Added inline CSS comments describing the auth layout and hero rendering strategy.
- Documented non-cropping image behavior and responsive chip wrapping.

## Active Asset
- Active image path: `Frontend/wwwroot/images/Gemini_Generated_Image_s4on5ys4on5ys4on-Photoroom.png`
- Referenced by:
  - Login hero image (`Login.cshtml`)
  - Favicon links (`Login.cshtml` and `_Layout.cshtml`)

## Design Intent
- Keep Bosconian identity visible before login.
- Preserve full logo visibility with `object-fit: contain`.
- Avoid color clash by using a neutral gradient panel behind the hero image.
- Maintain readability and responsive behavior on narrow viewports.

## Verification Checklist
1. Open `/login` and confirm the hero logo is visible.
2. Confirm browser tab shows the crest favicon.
3. Open any authenticated page and confirm the same favicon appears.
4. Resize viewport and verify value chips wrap cleanly.

## Notes
- The favicon currently uses a PNG source for simplicity and consistency with the hero asset.
- If needed later, generate a dedicated `.ico` file for broader legacy browser compatibility.
