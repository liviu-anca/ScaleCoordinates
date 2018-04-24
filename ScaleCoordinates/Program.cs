using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;

namespace ScaleCoordinates
{
    class Program
    {
        const string NormalizeFrom = "normalize_from";
        const string DenormalizeTo = "denormalize_to";
        static readonly string[] Actions = new[] { NormalizeFrom, DenormalizeTo };
        static readonly string UsageMsg =
$@"Usage:
    ScaleCoordinates <input_xaml_file_path> <output_xaml_file_path> ({NormalizeFrom}|{DenormalizeTo})=<scaling>
or:
    ScaleCoordinates <folder_path> ({NormalizeFrom}|{DenormalizeTo})=<scaling>";
        static readonly string InvalidScalingMsg = "Invalid value for parameter 'scaling'";
        static readonly string FolderProcessingConfirmationMsg =
@"Folder processing will modify all the XAML files in the given path.
Please enter 'yes' to confirm or 'no' to cancel.";

        static void Main(string[] args)
        {
            // parse input and validate

            if (args.Length < 2 || args.Length > 3)
            {
                Console.WriteLine(UsageMsg);
                return;
            }

            string operation = string.Empty;
            var processFolder = false;

            if (args.Length == 2)
            {
                // assume we should process a folder
                if (!Directory.Exists(args[0]))
                {
                    Console.WriteLine(UsageMsg);
                    return;
                }

                operation = args[1];
                processFolder = true;
            }
            else // args.Length = 3
            {
                operation = args[2];
                processFolder = false;
            }

            // parse and validate 'scaling' value

            var parts = operation.Split('=');

            if (parts.Length != 2 || !Actions.Contains(parts[0]))
            {
                Console.WriteLine(UsageMsg);
                return;
            }

            if (!int.TryParse(parts[1], out var scaling) || scaling < 100 || scaling > 500)
            {
                Console.WriteLine(InvalidScalingMsg);
                return;
            }

            var factor = parts[0] == NormalizeFrom
                ? 100.0 / scaling // normalize from non-standard 'scaling' to 100%
                : scaling / 100.0; // de-normalize from 100% to non-standard scaling

            // do the processing 

            if (processFolder)
            {
                // processing would be made in-place, ask for confirmation
                Console.WriteLine(FolderProcessingConfirmationMsg);
                var confirm = Console.ReadLine();
                if (confirm.ToUpper() != "YES")
                    return;

                ProcessFolder(args[0], factor);
            }
            else
            {
                // validate File existence
                if (!File.Exists(args[0]))
                {
                    Console.WriteLine(UsageMsg);
                    return;
                }

                ProcessFile(args[0], args[1], factor);
            }
        }

        static void ProcessFile(string inputFilePath, string outputFilePath, double factor)
        {
            Console.WriteLine($"Processing '{inputFilePath}' to '{outputFilePath}' with {factor:0.###}");

            try
            {
                // open input file

                var doc = new XmlDocument();
                doc.Load(inputFilePath);
                var root = doc.DocumentElement;
                var nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("ui", "http://schemas.uipath.com/workflow/activities");

                // process CursorPosition elements

                var positionNodes = root.SelectNodes(
                    @"//ui:Click/ui:Click.CursorPosition/ui:CursorPosition |
                      //ui:Hover/ui:Hover.CursorPosition/ui:CursorPosition |
                      //ui:ClickText/ui:ClickText.CursorPosition/ui:CursorPosition |
                      //ui:HoverText/ui:HoverText.CursorPosition/ui:CursorPosition |
                      //ui:ClickImage/ui:ClickImage.CursorPosition/ui:CursorPosition |
                      //ui:HoverImage/ui:HoverImage.CursorPosition/ui:CursorPosition |
                      //ui:ClickOCRText/ui:ClickOCRText.CursorPosition/ui:CursorPosition |
                      //ui:HoverOCRText/ui:HoverOCRText.CursorPosition/ui:CursorPosition |
                      //ui:FindRelative/ui:FindRelative.CursorPosition/ui:CursorPosition",
                    nsmgr);

                foreach (var node in positionNodes)
                {
                    var xmlNode = node as XmlNode;
                    var attributes = xmlNode.Attributes;
                    var offsetX = attributes["OffsetX"];
                    var offsetY = attributes["OffsetY"];

                    if (offsetX != null && !string.IsNullOrEmpty(offsetX.Value) && int.TryParse(offsetX.Value, out var x))
                        offsetX.Value = ((int)Math.Round(factor * x)).ToString(CultureInfo.InvariantCulture);
                    if (offsetY != null && !string.IsNullOrEmpty(offsetY.Value) && int.TryParse(offsetY.Value, out var y))
                        offsetY.Value = ((int)Math.Round(factor * y)).ToString(CultureInfo.InvariantCulture);
                }

                // process Region elements

                var regionNodes = root.SelectNodes(
                    @"//ui:Target/ui:Target.ClippingRegion/ui:Region |
                      //ui:ClickTrigger/ui:ClickTrigger.ClippingRegion/ui:Region |
                      //ui:ClickImageTrigger/ui:ClickImageTrigger.ClippingRegion/ui:Region |
                      //ui:SetClippingRegion/ui:SetClippingRegion.Size/ui:Region |
                      //ui:ElementRecordingInfo/ui:ElementRecordingInfo.ElementClippingRegion/ui:Region",
                    nsmgr);

                foreach (var node in regionNodes)
                {
                    var xmlNode = node as XmlNode;
                    var attributes = xmlNode.Attributes;
                    var rectangle = attributes["Rectangle"];

                    if (rectangle != null && !string.IsNullOrEmpty(rectangle.Value))
                    {
                        var values = rectangle.Value.Split(',');

                        if (values.Length == 4)
                        {
                            if (int.TryParse(values[0], out var x) &&
                                int.TryParse(values[1], out var y) &&
                                int.TryParse(values[2], out var width) &&
                                int.TryParse(values[3], out var height))
                                rectangle.Value = string.Join(", ",
                                    new[] { (int)Math.Round(factor * x), (int)Math.Round(factor * y), (int)Math.Round(factor * width), (int)Math.Round(factor * height) }
                                    .Select(v => v.ToString(CultureInfo.InvariantCulture)));
                        }
                    }
                }

                // save output file

                doc.Save(outputFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static void ProcessFolder(string folderPath, double factor)
        {
            try
            {
                // process XAML files
                var files = Directory.GetFiles(folderPath, "*.xaml");
                foreach (var file in files)
                    ProcessFile(file, file, factor); // do in-place processing

                // process subfolders
                var folders = Directory.GetDirectories(folderPath);
                foreach (var folder in folders)
                    ProcessFolder(folder, factor); // recursive call
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
