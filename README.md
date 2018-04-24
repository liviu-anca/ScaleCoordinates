# UiPath ScaleCoordinates tool
A command-line tool for scaling elements of type CursorPosition and Region in a XAML workflow file.

## Getting Started
Download the desired version module from the [Releases](https://github.com/UiPath/ScaleCoordinates/releases) page.

## Usage
Open a Command Prompt window and type:
```CMD
ScaleCoordinates <input_xaml_file_path> <output_xaml_file_path> normalize_from=<scaling>
```
to normalize coordinates from a non-standard scaling (between 100% and 500%, other than 100%) to standard scaling (100%)
or:
```CMD
ScaleCoordinates <input_xaml_file_path> <output_xaml_file_path> denormalize_to=<scaling>
```
to de-normalize coordinates from standard scaling (100%) to a non-standard scaling (between 100% and 500%, other than 100%)

`<input_xaml_file_path>`: path to XAML source file

`<output_xaml_file_path>`: path to XAML destination file

`<scaling>`: non-standard scaling value to normalize coordinates from or to de-normalize coordinates to.

#### Example:

```CMD
ScaleCoordinates "C:\UiPath\Projects\Activities\Main.xaml" "C:\UiPath\Projects\Activities_fixed\Main.xaml" normalize_from=150
```
This command will scale the coordinates by a factor of 0.667 (100/150). It would be useful if the workflow file was saved at a scale of 150% in a previous version (18.1.x) and run at a scale of 100% in the current version (18.2.x).

## License
This project is copyright [UiPath INC](https://uipath.com) and licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.
