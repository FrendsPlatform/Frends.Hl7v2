# Changelog

## [Unreleased]

### Changed

- Check cancellation during XML reading and traversal, between segments and fields,
  and between conversion stages. Propagate cancellation independently of error-handling options.
- Index XML group children in one forward pass to avoid quadratic repeated-group
  scans, while retaining NHapi field parsing and special segment-alias handling.
- Supply indexed child-node snapshots during read-only mapping to remove quadratic
  traversal from NHapi's remaining field and datatype loops without forking the dependency.

## [1.2.0] - 2026-08-06

### Changed

- Updated copyright information to comply with Frends platform standards.

## [1.1.0] - 2026-07-03

### Added

- MSH field overrides (MshOverrides): ability to overwrite any MSH field (MSH-1 to MSH-15) in the output HL7v2 message
CR option to LineEnding enum

## [1.0.0] - 2026-02-27

### Added

- Initial implementation
