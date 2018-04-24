using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;

namespace ScaleCoordinates
{
    class Program
    {
        const string normalizeFrom = "normalize_from";
        const string denormalizeTo = "denormalize_to";
        static readonly string[] actions = new[] { normalizeFrom, denormalizeTo };
        static readonly string usageMsg = $@"
Usage:
    ScaleCoordinates <input_xaml_file_path> <output_xaml_file_path> ({normalizeFrom}|{denormalizeTo})=<scaling>
or:
    ScaleCoordinates <folder_path> ({normalizeFrom}|{denormalizeTo})=<scaling>
";
        static readonly string invalidScaling = "Invalid value for parameter 'scaling'";

        static void Main(string[] args)
        {
            // parse input and validate

            if (args.Length < 1)
            {
                Console.WriteLine(usageMsg);
                return;
            }

            string operation = string.Empty;
            var processFolder = false;

            if (Directory.Exists(args[0]))
            {
                // we have only 1 more arg
                if (args.Length != 2)
                {
                    Console.WriteLine(usageMsg);
                    return;
                }
                operation = args[1];
                processFolder = true;
            }
            else
            {
                // assume it's a file path; in this case, we have 2 more args
                if (args.Length != 3)
                {
                    Console.WriteLine(usageMsg);
                    return;
                }
                operation = args[2];
                processFolder = false;
            }

            var parts = operation.Split('=');

            if (parts.Length != 2 || !actions.Contains(parts[0]))
            {
                Console.WriteLine(usageMsg);
                return;
            }

            if (!int.TryParse(parts[1], out var scaling) || scaling < 100 || scaling > 500)
            {
                Console.WriteLine(invalidScaling);
                return;
            }

            var factor = parts[0] == normalizeFrom
                ? 100.0 / scaling // normalize from non-standard 'scaling' to 100%
                : scaling / 100.0; // de-normalize from 100% to non-standard scaling

            if (processFolder)
                ProcessFolder(args[0], factor);
            else
                ProcessFile(args[0], args[1], factor);
        }

        static void ProcessFile(string inputFilePath, string outputFilePath, double factor)
        {
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

        }
    }
}
