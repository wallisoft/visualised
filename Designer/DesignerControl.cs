using System;

namespace ConfigUI.Designer
{
    public class DesignerControl
    {
        public int? DatabaseId { get; set; }
        public string Type { get; set; } = "label";
        public string Name { get; set; } = "Control";
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; } = 100;
        public double Height { get; set; } = 30;
        public bool Visible { get; set; } = true;
        public bool Enabled { get; set; } = true;
        public string? BackgroundColor { get; set; }
        public string? ForegroundColor { get; set; }
        public string? FontFamily { get; set; }
        public double? FontSize { get; set; } = 12;
        public bool FontBold { get; set; } = false;
	public bool FontItalic { get; set; } = false;    
        public bool FontUnderline { get; set; } = false;  
        public string? Caption { get; set; }
        public string? Text { get; set; }
        public string Alignment { get; set; } = "Left";
        public int Interval { get; set; } = 1000;
    }
}
