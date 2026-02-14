using System.Drawing;
using System.Text;
using Cocona;

namespace GenerateKenneyUSS
{

    class Program
    {
        // Adjust these to match your Kenney asset padding
        const int SliceBorder = 8;

        // Tint color for active state (darker)
        const int DarkTint = 180;

        // Additional padding for tab headers, buttons, etc.
        const int Padding = 8;

        static void Main(string[] args)
        {
            // Cocona.Lite uses the static CoconaLiteApp class
            var app = CoconaLiteApp.Create();

            // You can define commands using a simple method delegate
            app.AddCommand((
                [Option('p', Description = "The target directory to process")] string path,
                [Option('r', Description = "Package root directory. By default it is parent directory from the path argument.")] string? root = null) =>
            {
                path = Path.GetFullPath(path);
                if (root == null)
                {
                    root = Path.GetDirectoryName(path);
                    while (root != null && !File.Exists(Path.Combine(root, "package.json")))
                    {
                        root = Path.GetDirectoryName(root);
                    }
                    if (root == null)
                        throw new Exception("Could not find package.json in any parent directory, please provide a valid package root.");
                }
                else
                {
                    if (!File.Exists(Path.Combine(root, "package.json")))
                        throw new Exception("Provided root directory does not contain package.json, please provide a valid package root.");
                }

                Console.WriteLine($"Processing directory {path} from package {root}");

                if (!Directory.Exists(path))
                {
                    Console.WriteLine("Invalid path.");
                    return;
                }

                string getAssetName(string filePath)
                {
                    filePath = Path.GetFullPath(filePath);
                    var relPath = Path.GetRelativePath(root, filePath).Replace("\\", "/");
                    return "project://database/Packages/nl.kenney.uipack/" + relPath;
                }

                StringBuilder ussContent = new StringBuilder();

                var files = Directory.GetFiles(path, "*.png", SearchOption.AllDirectories);

                /// Collect all buttons into single demo document.
                var buttonDemoUxml = new StringBuilder();
                buttonDemoUxml.AppendLine("<ui:UXML xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:ui=\"UnityEngine.UIElements\" xmlns:uie=\"UnityEditor.UIElements\" noNamespaceSchemaLocation=\"../UIElementsSchema/UIElements.xsd\" editor-extension-mode=\"False\">");
                buttonDemoUxml.AppendLine("    <Style src=\"project://database/Packages/nl.kenney.uipack/UIPack/Styles.uss?fileID=7433441132597879392&amp;guid=6b2ba52b893135e4bbb95e1628538ea0&amp;type=3#Styles\"/>");
                buttonDemoUxml.AppendLine("    <ui:ScrollView>");
                buttonDemoUxml.AppendLine("    <ui:VisualElement style=\"flex-direction: row; flex-wrap: wrap;\">");

                foreach (var file in files)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    string dir = Path.GetDirectoryName(file) ?? "";
                    if (!Path.GetFileName(dir).Equals("Default", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string colorDir = Path.GetFileName(Path.GetDirectoryName(dir) ?? "");

                    var className = $".{colorDir.ToLower()}.{fileName}";

                    var assetName = getAssetName(file);
                    var disabledName = getAssetName(file.Replace(colorDir, "Grey"));

                    // Logic for Rounded (Unresizable)
                    if (fileName.StartsWith("button_round", StringComparison.OrdinalIgnoreCase))
                    {
                        buttonDemoUxml.AppendLine($"        <ui:Button class=\"{colorDir.ToLower()} {fileName}\" text=\"{fileName}\" />");
                        GenerateRoundStyle(ussContent, className, assetName, disabledName, file);
                    }
                    // Logic for Rect/Square (9-Slice)
                    else if (fileName.StartsWith("button_rectangle", StringComparison.OrdinalIgnoreCase) ||
                             fileName.StartsWith("button_square", StringComparison.OrdinalIgnoreCase))
                    {
                        buttonDemoUxml.AppendLine($"        <ui:Button class=\"{colorDir.ToLower()} {fileName}\" text=\"{fileName}\" />");
                        Generate9SliceStyle(ussContent, className, assetName, disabledName, file);
                    }
                    // Logic checkboxes.
                    else if (fileName.StartsWith("check_square", StringComparison.OrdinalIgnoreCase) && !fileName.EndsWith("_cross") && !fileName.EndsWith("_square"))
                    {
                        // Skip checkmark images, we only want the box for styling
                        if (fileName.EndsWith("_checkmark"))
                            continue;
                        var checkedAssetName = getAssetName(file.Replace(fileName, fileName + "_checkmark"));
                        var disabledCheckedName = getAssetName(file.Replace(colorDir, "Grey").Replace(fileName, fileName + "_checkmark"));

                        GenerateCheckboxStyle(ussContent, className, assetName, checkedAssetName, disabledName, disabledCheckedName, file);
                    }
                    else if (fileName.Equals("slide_vertical_color", StringComparison.OrdinalIgnoreCase))
                    {
                        var buttonLow = getAssetName(Path.Combine(dir, "arrow_basic_n_small.png"));
                        var buttonHigh = getAssetName(Path.Combine(dir, "arrow_basic_s_small.png"));
                        var knob = getAssetName(Path.Combine(dir, "slide_vertical_color_section.png"));
                        GenerateVerticalSliderStyle(ussContent, className, buttonLow, buttonHigh, assetName, knob);
                    }
                }

                // Finalize and save the button demo UXML
                buttonDemoUxml.AppendLine("    </ui:VisualElement>");
                buttonDemoUxml.AppendLine("    </ui:ScrollView>");
                buttonDemoUxml.AppendLine("</ui:UXML>");
                File.WriteAllText(Path.Combine(path, "Buttons.uxml"), buttonDemoUxml.ToString());

                // Save the generated USS content to a file
                File.WriteAllText(Path.Combine(path, "Styles.uss"), ussContent.ToString());
            });

            app.Run();
        }

        private static void GenerateVerticalSliderStyle(StringBuilder ussContent, string className, string buttonLow, string buttonHigh, string tracker, string dragger)
        {
            ussContent.AppendLine($"{className} .unity-scroller--vertical > .unity-scroller__low-button");
            ussContent.AppendLine("{");
            ussContent.AppendLine($"    background-image: url('{buttonLow}');");
            ussContent.AppendLine("    -unity-background-image-tint-color: rgb(255, 255, 255);");
            ussContent.AppendLine("    background-color: transparent;");
            ussContent.AppendLine("    border-width: 0;");
            ussContent.AppendLine("}");
            ussContent.AppendLine("");
            ussContent.AppendLine($"{className} .unity-scroller--vertical > .unity-scroller__high-button");
            ussContent.AppendLine("{");
            ussContent.AppendLine($"    background-image: url('{buttonHigh}');");
            ussContent.AppendLine("    -unity-background-image-tint-color: rgb(255, 255, 255);");
            ussContent.AppendLine("    background-color: transparent;");
            ussContent.AppendLine("    border-width: 0;");
            ussContent.AppendLine("}");
            ussContent.AppendLine("");
            ussContent.AppendLine($"{className} .unity-base-slider--vertical .unity-base-slider__tracker");
            ussContent.AppendLine("{");
            ussContent.AppendLine($"    background-image: url('{tracker}');");
            ussContent.AppendLine("    -unity-slice-left: 8;");
            ussContent.AppendLine("    -unity-slice-right: 8;");
            ussContent.AppendLine("    -unity-slice-top: 32;");
            ussContent.AppendLine("    -unity-slice-bottom: 32;");
            ussContent.AppendLine("    border-width: 0;");
            ussContent.AppendLine("    background-color: transparent;");
            ussContent.AppendLine("}");
            ussContent.AppendLine("");
            ussContent.AppendLine($"{className} .unity-base-slider--vertical .unity-base-slider__dragger");
            ussContent.AppendLine("{");
            ussContent.AppendLine($"    background-image: url('{dragger}');");
            ussContent.AppendLine("    -unity-slice-left: 8;");
            ussContent.AppendLine("    -unity-slice-right: 8;");
            ussContent.AppendLine("    -unity-slice-top: 8;");
            ussContent.AppendLine("    -unity-slice-bottom: 8;");
            ussContent.AppendLine("    background-color: transparent;");
            ussContent.AppendLine("}");


        }

        static void GenerateCheckboxStyle(StringBuilder sb, string className, string path, string checkedPath, string disabled, string disabledChecked, string fileName)
        {
            var bmp = new BitmapInfo(fileName);

            sb.AppendLine($"{className}{{");
            sb.AppendLine("}\n");

            sb.AppendLine($"{className} .unity-toggle__checkmark{{");
            sb.AppendLine($"    background-image: url('{path}');");
            sb.AppendLine($"    -unity-background-image-tint-color: rgb(255, 255, 255);");
            sb.AppendLine($"    min-width: {bmp.Width}px;");
            sb.AppendLine($"    min-height: {bmp.Height}px;");
            sb.AppendLine($"    border-width: 0;");
            sb.AppendLine($"    background-color: transparent;");
            sb.AppendLine("}\n");

            sb.AppendLine($"{className}:checked .unity-toggle__checkmark{{");
            sb.AppendLine($"    background-image: url('{checkedPath}');");
            sb.AppendLine($"    -unity-background-image-tint-color: rgb(255, 255, 255);");
            sb.AppendLine($"    background-color: transparent;");
            sb.AppendLine("}\n");

            sb.AppendLine($"{className}:disabled .unity-toggle__checkmark{{");
            sb.AppendLine($"    background-image: url('{disabled}');");
            sb.AppendLine($"    -unity-background-image-tint-color: rgb(255, 255, 255);");
            sb.AppendLine($"    background-color: transparent;");
            sb.AppendLine($"    opacity: 1;");
            sb.AppendLine("}\n");

            sb.AppendLine($"{className}:checked:disabled .unity-toggle__checkmark{{");
            sb.AppendLine($"    background-image: url('{disabledChecked}');");
            sb.AppendLine($"    -unity-background-image-tint-color: rgb(255, 255, 255);");
            sb.AppendLine($"    background-color: transparent;");
            sb.AppendLine($"    opacity: 1;");
            sb.AppendLine("}\n");
        }
        static void Generate9SliceStyle(StringBuilder sb, string className, string path, string disabled, string fileName)
        {
            var bmp = new BitmapInfo(fileName);

            sb.AppendLine($"{className} {{");
            sb.AppendLine($"    background-image: url('{path}');");
            sb.AppendLine($"    -unity-slice-left: {SliceBorder};");
            sb.AppendLine($"    -unity-slice-right: {SliceBorder};");
            sb.AppendLine($"    -unity-slice-top: {SliceBorder};");
            sb.AppendLine($"    -unity-slice-bottom: {SliceBorder};");
            sb.AppendLine($"    min-width: {bmp.Width}px;");
            //sb.AppendLine($"    min-height: {bmp.Height}px;");
            sb.AppendLine($"    color: rgb({bmp.FontColor.R}, {bmp.FontColor.G}, {bmp.FontColor.B});");
            sb.AppendLine($"    -unity-font-style: bold;");
            sb.AppendLine($"    border-width: 0;");
            sb.AppendLine($"    background-color: transparent;");
            sb.AppendLine("}\n");

            //sb.AppendLine($"Button{className} {{");
            //sb.AppendLine($"    font-size: {Math.Max(4,(bmp.Height-SliceBorder)/2)}px;");
            //sb.AppendLine("}\n");

            sb.AppendLine($"Button{className}:hover {{");
            sb.AppendLine($"    translate: 0 -2px;");
            sb.AppendLine("}\n");

            sb.AppendLine($"Button{className}:active {{");
            sb.AppendLine($"    -unity-background-image-tint-color: rgb({DarkTint}, {DarkTint}, {DarkTint});");
            sb.AppendLine($"    translate: 0 2px;");
            sb.AppendLine("}\n");

            sb.AppendLine($"{className}:disabled {{");
            sb.AppendLine($"    background-image: url('{disabled}');");
            sb.AppendLine($"    color: rgb(204, 204, 204);");
            sb.AppendLine($"    opacity: 1;");
            sb.AppendLine("}\n");
            sb.AppendLine($"{className}:disabled:hover {{");
            sb.AppendLine($"    translate: 0 0;");
            sb.AppendLine("}\n");

            sb.AppendLine($"TabView{className}{{");
            sb.AppendLine($"    background-image: none;");
            sb.AppendLine("}\n");

            sb.AppendLine($"TabView{className} Tab{{");
            sb.AppendLine($"    background-image: url('{path}');");
            sb.AppendLine($"    -unity-slice-left: {SliceBorder};");
            sb.AppendLine($"    -unity-slice-right: {SliceBorder};");
            sb.AppendLine($"    -unity-slice-top: {SliceBorder};");
            sb.AppendLine($"    -unity-slice-bottom: {SliceBorder};");
            sb.AppendLine($"    padding: {SliceBorder}px;");
            sb.AppendLine("}\n");

            sb.AppendLine($"TabView{className} .unity-tab__header{{");
            sb.AppendLine($"    background-image: url('{path}');");
            sb.AppendLine($"    -unity-slice-left: {SliceBorder};");
            sb.AppendLine($"    -unity-slice-right: {SliceBorder};");
            sb.AppendLine($"    -unity-slice-top: {SliceBorder};");
            sb.AppendLine($"    -unity-slice-bottom: {SliceBorder};");
            sb.AppendLine($"    min-width: {bmp.Width}px;");
            //sb.AppendLine($"    min-height: {bmp.Height}px;");
            //sb.AppendLine($"    font-size: {Math.Max(4, (bmp.Height - SliceBorder) / 2)}px;");
            sb.AppendLine($"    color: rgb({bmp.FontColor.R}, {bmp.FontColor.G}, {bmp.FontColor.B});");
            sb.AppendLine($"    -unity-font-style: bold;");
            sb.AppendLine($"    border-width: 0;");
            sb.AppendLine($"    background-color: transparent;");
            //sb.AppendLine($"    margin-top: 0px;");
            //sb.AppendLine($"    translate: 0 0px;");
            sb.AppendLine($"    padding: {Padding}px;");
            sb.AppendLine($"    padding-bottom: {Padding + SliceBorder}px;");
            sb.AppendLine($"    margin-left: {0}px;");
            sb.AppendLine($"    margin-right: {Padding}px;");
            sb.AppendLine($"    margin-top: -{SliceBorder}px;");
            sb.AppendLine($"    translate: 0 {SliceBorder}px;");
            sb.AppendLine($"    -unity-background-image-tint-color: rgb({DarkTint}, {DarkTint}, {DarkTint});");
            sb.AppendLine("}\n");
            sb.AppendLine($"TabView{className} .unity-tab__header:checked{{");
            sb.AppendLine($"    -unity-background-image-tint-color: rgb(255, 255, 255);");
            sb.AppendLine("}\n");

        }

        static void GenerateRoundStyle(StringBuilder sb, string className, string path, string disabled, string fileName)
        {
            var bmp = new BitmapInfo(fileName);
            sb.AppendLine($"{className} {{");
            sb.AppendLine($"    background-image: url('{path}');");
            sb.AppendLine($"    width: {bmp.Width}px;");
            sb.AppendLine($"    height: {bmp.Height}px;");
            sb.AppendLine($"    color: rgb({bmp.FontColor.R}, {bmp.FontColor.G}, {bmp.FontColor.B});");
            sb.AppendLine($"    border-width: 0;");
            sb.AppendLine($"    background-color: transparent;");
            sb.AppendLine("    flex-grow: 0;");
            sb.AppendLine("    flex-shrink: 0;");
            sb.AppendLine("}\n");

            sb.AppendLine($"{className}:disabled {{");
            sb.AppendLine($"    background-image: url('{disabled}');");
            sb.AppendLine($"    opacity: 1;");
            sb.AppendLine("}\n");

            sb.AppendLine($"{className}:hover {{");
            sb.AppendLine($"    translate: 0 -2px;");
            sb.AppendLine("}\n");

            sb.AppendLine($"{className}:active {{");
            sb.AppendLine($"    -unity-background-image-tint-color: rgb(180, 180, 180);");
            sb.AppendLine($"    translate: 0 2px;");
            sb.AppendLine("}\n");
        }
        public class BitmapInfo
        {
            public BitmapInfo(string filePath)
            {
                using var bitmap = (Bitmap)Bitmap.FromFile(filePath);
                if (bitmap == null)
                    throw new Exception($"Failed to load image: {filePath}");
                Width = bitmap.Width;
                Height = bitmap.Height;
                CenterColor = bitmap.GetPixel(bitmap.Width / 2, bitmap.Height / 2);
                double luminosity = (0.299 * CenterColor.R +
                                     0.587 * CenterColor.G +
                                     0.114 * CenterColor.B) / 255;

                FontColor = luminosity > 0.6 ? Color.Black : Color.White;
            }

            public Color CenterColor { get; }
            public int Width { get; }
            public int Height { get; }
            public Color FontColor { get; }
        }
    }

}