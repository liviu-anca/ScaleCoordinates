﻿using System;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace ScaleCoordinates
{
    class Program
    {
        static void Main(string[] args)
        {
            // parse input and validate

            if (args.Length != 4)
            {
                Console.WriteLine("Usage: ScaleCoordinates <xaml_file_path_in> <xaml_file_path_out> <multiply> <divide>");
                return;
            }

            var filePathIn = args[0];
            var filePathOut = args[1];

            if (!double.TryParse(args[2], out var mul) || !double.TryParse(args[3], out var div) || mul <= 0 || div <= 0)
            {
                Console.WriteLine("Invalid arguments");
                return;
            }

            var factor = mul / div;

            try
            {
                // open input file

                var doc = new XmlDocument();
                doc.Load(filePathIn);
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
                        offsetX.Value = ((int)(factor * x)).ToString(CultureInfo.InvariantCulture);
                    if (offsetY != null && !string.IsNullOrEmpty(offsetY.Value) && int.TryParse(offsetY.Value, out var y))
                        offsetY.Value = ((int)(factor * y)).ToString(CultureInfo.InvariantCulture);
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
                                    new[] { (int)(factor * x), (int)(factor * y), (int)(factor * width), (int)(factor * height) }
                                    .Select(v => v.ToString(CultureInfo.InvariantCulture)));
                        }
                    }
                }

                // save output file

                doc.Save(filePathOut);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}