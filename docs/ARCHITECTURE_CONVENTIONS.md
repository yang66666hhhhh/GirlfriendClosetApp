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
- Use cases that orchestrate multiple services (e.g. `GetTodayRecommendations`) should accept a request DTO and return a result DTO.

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
- `RecommendationRotationStrategy` is a Domain enum (`ClosetApp.Domain/Enums/`), not an Infrastructure type.
- All are registered as Singletons in DI.

## Shared Components

- `EnumRadioGroup<TEnum>` — generic RadioButton selection group with `IEnumRadioGroup` non-generic interface for WPF binding.
- `ThemeCard` — custom UserControl for theme selection with `IsSelected` DependencyProperty driving visual state.
- `FileSizeFormatter` — static utility for human-readable file sizes (B/KB/MB/GB).
- `AnimationHelper` — static `Shake(UIElement)` method for validation error feedback.
- `ThemeColorHelper` — theme-aware color resolution and blending utilities.
- Place reusable UI utilities in `Components/Shared/`; prefer DependencyProperties over imperative code-behind for custom controls.

## Safe Delete Pattern

- `IImageStorageService.TryDeleteImageAsync(string?)` follows a safe-delete pattern: ignores null/empty paths and swallows exceptions with a warning log.
- Use this pattern in ViewModels and UseCases where image deletion is a side effect, not the primary action.
