# Architecture Conventions

## Editor Panels

- New editor panels implement `IEditorPanel<T>`.
- Editor completion uses `EditorResult<T>` with `Saved`, `Deleted`, or `Cancelled`.
- Views open editor panels through `EditorModal.Show(...)`; direct `ModalService.Show/Hide` belongs inside modal infrastructure only.

## Page State

- Tab pages use thin state classes in `ClosetApp.UI/States`.
- State classes own search text, filters, loading flags, empty state, and current item collections.
- Code-behind may still own click handlers, animations, visual tree probing, and modal orchestration.

## Design System

- Design tokens live in `ClosetApp.UI/Themes/Tokens`.
- Control styles live in `ClosetApp.UI/Themes/Controls`.
- `Themes/Colors.xaml` is a compatibility forwarder and must not reintroduce alternate `PrimaryBrush` definitions.
- Prefer token resources over hard-coded color, spacing, radius, shadow, and motion values in new UI.

## Application Use Cases

- Existing services remain available for CRUD and repository-facing operations.
- New business workflows should start in `ClosetApp.Application/UseCases`.
- Use cases should be named in product language, such as `GetWardrobeOverview`, `RecordOutfitWorn`, or `GetTagsForSelection`.

## Namespace Safety

- Domain entities that collide with UI component namespaces should use explicit aliases.
- Preferred aliases:
  - `ClothingEntity = global::ClosetApp.Domain.Entities.Clothing`
  - `OutfitEntity = global::ClosetApp.Domain.Entities.Outfit`
  - `TagEntity = global::ClosetApp.Domain.Entities.Tag`
- Avoid creating new UI namespaces that are identical to domain entity names unless the file already lives in that feature area.

## Tags

- Tags are platform metadata, not clothing-only details.
- Reusable tag UI belongs under `Components/Tags`.
- Tag selection APIs should take a `TagCategory` so they can be reused by clothing, outfits, scenes, and future calendar features.

## Error Presentation

- Use `WardrobeActionErrorPresenter` for all user-facing error messages.
- It classifies exceptions into: database busy, file occupied, permission denied, validation failure, and unknown.
- Each method returns `(string Title, string Detail)` for direct display in toast or modal.
- Prefer this over raw exception messages in UI code.

## Batch Import

- Batch import logic lives in `Components/Clothing/Batch*` files.
- `BatchClothingImportBuilder` scans images and builds preview items.
- `BatchImportDuplicateChecker` detects same-name/same-size risks before import.
- `BatchClothingImportSummaryBuilder` builds result summaries after import.
- The UseCase `ImportClothesFromImages` orchestrates the actual import.
- These files are also linked into `ClosetApp.UI.Logic` for testability.

## Preferences Services

- User preferences (theme, weather city, recommendation settings) are persisted as JSON in `%LocalAppData%\ClosetApp\`.
- `ThemePreferencesService` manages theme selection (Rose/Blue).
- `WeatherPreferencesService` manages weather city preference.
- `RecommendationPreferencesService` manages recommendation rotation strategy and scene preference.
- All are registered as Singletons in DI.
