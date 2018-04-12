# UiPath ScaleCoordinates tool
A command-line tool for scaling elements of type CursorPosition and Region in a XAML workflow file.

## Getting Started
Download the desired version module from the [Releases](https://github.com/UiPath/ScaleCoordinates/releases) page.

## Usage
Open a Command Prompt window and type:
```CMD
ScaleCoordinates <xaml_file_path_in> <xaml_file_path_out> <multiply> <divide>
```
`<xaml_file_path_in>`: path to XAML source file

`<xaml_file_path_out>`: path to XAML destination file

The initial value will be multiplied by `<multiply>` and divided by `<divide>`.

#### Example:

```CMD
ScaleCoordinates "C:\UiPath\Projects\Activities\Main.xaml" "C:\UiPath\Projects\Activities_fixed\Main.xaml" 100 150
```
This command will scale the coordinates by a factor of 0.667 (100/150). It would be useful if the workflow file was saved at a scale of 150% in a previous version (18.1.x) and run at a scale of 100% in the current version (18.2.x).

## License
This project is copyright [UiPath INC](https://uipath.com) and licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.
