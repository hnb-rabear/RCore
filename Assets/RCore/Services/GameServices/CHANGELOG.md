# Changelog

## [1.0.2]
- `ShowInAppUpdate(false)` now uses the flexible flow (background download) instead of the immediate flow. The downloaded update installs on the next `ShowInAppUpdate` call.
- `ShowInAppUpdate(true)` resumes an immediate update that the user left before it finished.
- Removed the `CompleteUpdate()` call after an immediate update; it is only needed for flexible updates.
- Skips the update when Google Play does not allow the requested update type.

## [1.0.1]
- Maintenance update.

## [1.0.0]
- Initial release.
