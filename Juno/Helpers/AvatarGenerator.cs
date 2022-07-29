//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.Drawing.Drawing2D;
//using System.Drawing.Imaging;
//using System.Drawing.Text;
//using System.IO;

//// http://seventhcap.com/blog/2015/05/31/generating-text-based-avatar-images-in-csharp/
//// https://gist.github.com/craigomatic/c5d2363820aaa818dee1

//// https://docs.sixlabors.com/articles/imagesharp.drawing/gettingstarted.html
//// https://medium.com/agile-content-teamwork/captcha-generator-with-sixlabors-libs-on-net-core-3-c0d2600b01d5
//// https://wellsb.com/csharp/aspnet/generate-images-statiq-imagesharp


//namespace Juno.Helpers
//{
//    public class AvatarGenerator
//    {
//        private List<string> _BackgroundColours;

//        public AvatarGenerator()
//        {
//            _BackgroundColours = new List<string> { "B26126", "FFF7F2", "FFE8D8", "74ADB2", "D8FCFF" };
//        }

//        public Stream Generate(string firstName, string lastName)
//        {
//            var avatarString = string.Format("{0}{1}", firstName[0], lastName[0]).ToUpper();

//            var randomIndex = new Random().Next(0, _BackgroundColours.Count - 1);
//            var bgColour = _BackgroundColours[randomIndex];

//            var bmp = new Bitmap(192, 192);
//            var sf = new StringFormat();
//            sf.Alignment = StringAlignment.Center;
//            sf.LineAlignment = StringAlignment.Center;

//            var font = new Font("Arial", 48, FontStyle.Bold, GraphicsUnit.Pixel);
//            var graphics = Graphics.FromImage(bmp);

//            graphics.Clear((Color)new ColorConverter().ConvertFromString("#" + bgColour));
//            graphics.SmoothingMode = SmoothingMode.AntiAlias;
//            graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
//            graphics.DrawString(avatarString, font, new SolidBrush(Color.WhiteSmoke), new RectangleF(0, 0, 192, 192), sf);
//            graphics.Flush();

//            var ms = new MemoryStream();
//            bmp.Save(ms, ImageFormat.Png);

//            return ms;
//        }

//        public Stream Avatar(string firstName, string lastName)
//        {
//            var avatarString = string.Format("{0}{1}", firstName[0], lastName[0]).ToUpper();

//            var randomIndex = new Random().Next(0, _BackgroundColours.Count - 1);
//            var bgColour = _BackgroundColours[randomIndex];

//            using (var bitmap = new Bitmap(50, 50))
//            {
//                using (Graphics g = Graphics.FromImage(bitmap))
//                {
//                    g.Clear((Color)new ColorConverter().ConvertFromString("#" + bgColour));
//                    using (Brush b = new SolidBrush(ColorTranslator.FromHtml("#eeeeee")))
//                    {

//                        g.FillEllipse(b, 0, 0, 49, 49);
//                    }

//                    float emSize = 12;
//                    g.DrawString("AM", new Font(FontFamily.GenericSansSerif, emSize, FontStyle.Regular),
//                        new SolidBrush(Color.Black), 10, 15);
//                }

//                using (var memStream = new System.IO.MemoryStream())
//                {
//                    bitmap.Save(memStream, System.Drawing.Imaging.ImageFormat.Png);
//                    var result = this.File(memStream.GetBuffer(), "image/png");
//                    return result;
//                }
//            }
//        }
//    }
//}



//// https://swharden.com/blog/2021-08-01-maui-skia-console/
//// https://swharden.com/blog/2022-05-25-maui-graphics/

//using System;
//using System.Collections.Generic;
//using System.IO;
//using Microsoft.Maui.Graphics;
//using Microsoft.Maui.Graphics.Skia;

//namespace Juno.Helpers
//{
//    public class AvatarGenerator
//    {
//        private List<string> _BackgroundColours;

//        SkiaBitmapExportContext bmpContext = new(600, 400, 1.0f);
//        SizeF bmpSize;
//        ICanvas canvas;

//        public AvatarGenerator()
//        {
//            _BackgroundColours = new List<string> { "B26126", "FFF7F2", "FFE8D8", "74ADB2", "D8FCFF" };
//            bmpSize = new(bmpContext.Width, bmpContext.Height);
//            canvas = bmpContext.Canvas;
//        }

//        public void Draw()
//        {
//            // Draw on the canvas with abstract methods that are agnostic to the renderer
//            this.ClearBackground(canvas, bmpSize, Colors.Navy);
//            this.DrawBigTextWithShadow(canvas, "PR");
//            this.SaveFig(bmpContext, Path.GetFullPath("quickstart.jpg"));
//        }

//        public void ClearBackground(ICanvas canvas, SizeF bmpSize, Color bgColor)
//        {
//            canvas.FillColor = Colors.Navy;
//            canvas.FillRectangle(0, 0, bmpSize.Width, bmpSize.Height);
//        }

//        public void DrawBigTextWithShadow(ICanvas canvas, string text)
//        {
//            canvas.FontSize = 36;
//            canvas.FontColor = Colors.White;
//            canvas.SetShadow(offset: new SizeF(2, 2), blur: 1, color: Colors.Black);
//            canvas.DrawString(text, 20, 50, HorizontalAlignment.Left);
//        }

//        public void SaveFig(BitmapExportContext bmp, string filePath)
//        {
//            bmp.WriteToFile(filePath);
//            Console.WriteLine($"WROTE: {filePath}");
//        }

//    }
//}