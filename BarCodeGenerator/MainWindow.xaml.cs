using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using Color = System.Drawing.Color;
using MColor = System.Windows.Media.Color;
using FF = System.Drawing.FontFamily;
using Font = System.Drawing.Font;
using SharpVectors.Converters;
using QRCoder;

namespace BarCodeGenerator {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private string ip, caption, link;
        private byte gw, cmd, typ, tar;
        private bool showLink, showCaption;
        private readonly List<int> bcTargets = new[] { 0 }.ToList();
        private readonly List<int> devTargets = Enumerable.Range(1, 250).ToList();
        private readonly List<int> grpTargets = Enumerable.Range(0, 256).ToList();
        private readonly List<int> scnTargets = Enumerable.Range(1, 255).ToList();
        private readonly Color backColor = Color.White;
        private readonly Color barcodeColor = Color.Black;
        private readonly QRCodeGenerator qrGenerator = new();

        public MainWindow() => InitializeComponent();

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) {
            var h = e.NewSize.Height;
            captionPreview.FontSize = h * 0.05;
            footerPreview.FontSize = h * 0.025;
            logoPreview.Height = h * 0.1;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e) {
            gwBox.ItemsSource = Enumerable.Range(0, 256).ToList();
            targetBox.ItemsSource = Enumerable.Range(0, 256).ToList();

            ipBox.Text = ip = "192.168.1.85";
            gwBox.SelectedIndex = gw = 0;
            cmdBox.SelectedIndex = cmd = 0; cmd++;
            typeBox.SelectedIndex = typ = 0;
            targetBox.SelectedIndex = tar = 0;
            captionBox.Text = caption = ""; // _Hello_World_
            showLinkBox.IsChecked = showLink = true;
            showCaptionBox.IsChecked = showCaption = true;

            ipBox.TextChanged += (sender, e) => { ip = ipBox.Text; GenerateBarCode(); };
            gwBox.SelectionChanged += (sender, e) => { gw = (byte)gwBox.SelectedIndex; GenerateBarCode(); };
            cmdBox.SelectionChanged += (sender, e) => { cmd = (byte)cmdBox.SelectedIndex; cmd++; GenerateBarCode(); };
            typeBox.SelectionChanged += (sender, e) => TypeChanged();
            targetBox.SelectionChanged += (sender, e) => { if (targetBox.SelectedIndex >= 0) TargetChanged(); };
            captionBox.TextChanged += (sender, e) => { caption = string.IsNullOrWhiteSpace(captionBox.Text) ? null : captionBox.Text; GenerateBarCode(); };
            showCaptionBox.Checked += (sender, e) => { showCaption = true; GenerateBarCode(); };
            showCaptionBox.Unchecked += (sender, e) => { showCaption = false; GenerateBarCode(); };
            showLinkBox.Checked += (sender, e) => { showLink = true; GenerateBarCode(); };
            showLinkBox.Unchecked += (sender, e) => { showLink = false; GenerateBarCode(); };
            //saveButton.Click += (sender, e) => Save();
            printButton.Click += (sender, e) => Print();

            GenerateBarCode();
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e) {
            qrGenerator.Dispose();
        }


        private void TypeChanged() {
            typ = (byte)typeBox.SelectedIndex;
            typ = (byte)(typ == 3 ? 4 : typ < 0 ? 0 : typ > 4 ? 4 : typ);
            targetBox.IsEnabled = typ != 0;
            targetBox.SelectedIndex = -1;
            targetBox.ItemsSource = typ switch { 1 => devTargets, 2 => grpTargets, 4 => scnTargets, _ => bcTargets };
            targetBox.SelectedItem = 0;
            //GenerateBarCode();
        }

        private void TargetChanged() {
            tar = (byte)targetBox.SelectedIndex;
            tar = (byte)(typ switch { 1 => tar + 1, 2 => tar, 4 => tar + 1, _ => 0 });
            GenerateBarCode();
        }

        private void GenerateBarCode() {
            var s = link = $"http://{ip}/control?gw={gw}&cmd={cmd}&typ={typ}&tar={tar}";
            footerPreview.Text = linkBox.Text = s.Replace("?", "\n?");
            captionPreview.Text = caption;

            using QRCodeData qrCodeData = qrGenerator.CreateQrCode(s, QRCodeGenerator.ECCLevel.Q);
            using QRCode qrCode = new(qrCodeData);
            var qrCodeBitmap = qrCode.GetGraphic(32, barcodeColor, backColor, false);
            img.Source = qrCodeBitmap.ToBitmapSource();
        }

        #region save
        //private static string Filters(params string[] filter) => string.Join('|', filter);
        //private static string _(string s, params string[] ext)
        //    => s
        //    + " (" + string.Join(';', ext.Select(e => "*." + e))
        //    + ")|" + string.Join(';', ext.Select(e => "*." + e));
        //private void Save() {
        //    //SaveFileDialog d = new() {
        //    //    FileName = "",
        //    //    Filter = Filters(
        //    //        _("Windows Bitmap", "bmp"),
        //    //        _("Adobe PDF", "pdf"),
        //    //        _("PNG", "png"),
        //    //        _("GIF", "gif"),
        //    //        _("TIFF", "tif", "tiff"),
        //    //        _("JPEG", "jpg", "jpeg", "jpe", "jfif")
        //    //    ),
        //    //    //Filter = "Windows Bitmap (*.bmp)|*.bmp|Adobe PDF (*.pdf)|*.pdf|",
        //    //    FilterIndex = 0
        //    //};
        //    //d.FileName = "";
        //    //if (d.ShowDialog() == true) {
        //    //    // save as ...
        //    //    var ext = System.IO.Path.GetExtension(d.FileName)[1..];
        //    //    try {
        //    //        switch (ext) {
        //    //            case "pdf": barcode.SaveAsPdf(d.FileName); break;
        //    //            default: barcode.SaveAsImage(d.FileName); break;
        //    //        }
        //    //    }
        //    //    catch (Exception ex) {
        //    //        MessageBox.Show($"file exception '{ex.Message}'");
        //    //    }
        //    //}
        //}
        #endregion

        private void Print() {
            PrintDialog dlg = new();
            if (dlg.ShowDialog() != true) return;

            var width = dlg.PrintableAreaWidth;
            var height = dlg.PrintableAreaHeight;
            var m = Math.Min(width, height);
            var margin = new Thickness(m * 0.15);
            var captionfontsize = m * 0.05;
            var footerfontsize = m * 0.025;
            var logoheight = m * 0.1;

            using QRCodeData qrCodeData = qrGenerator.CreateQrCode(link, QRCodeGenerator.ECCLevel.Q);
            using QRCode qrCode = new(qrCodeData);
            var qrCodeBitmap = qrCode.GetGraphic(20, barcodeColor, backColor, true);

            Grid panel; {
                panel = new() {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Width = width,
                    Height = height
                };
                // rows
                {
                    panel.RowDefinitions.Add(new() { Height = new GridLength(1, GridUnitType.Star) });
                    //panel.RowDefinitions.Add(new() { Height = new GridLength(margin.Top) });
                    panel.RowDefinitions.Add(new() { Height = GridLength.Auto });
                    panel.RowDefinitions.Add(new() { Height = GridLength.Auto });
                    panel.RowDefinitions.Add(new() { Height = GridLength.Auto });
                    //panel.RowDefinitions.Add(new() { Height = new GridLength(1, GridUnitType.Star) });
                    panel.RowDefinitions.Add(new() { Height = GridLength.Auto });
                    //panel.RowDefinitions.Add(new() { Height = new GridLength(margin.Bottom) });
                    panel.RowDefinitions.Add(new() { Height = new GridLength(1, GridUnitType.Star) });
                }
                // columns
                {
                    panel.ColumnDefinitions.Add(new() { Width = new GridLength(margin.Left) });
                    panel.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
                    panel.ColumnDefinitions.Add(new() { Width = new GridLength(margin.Right) });
                }
            }

            // DO BUNT
            {
                //var rnd = new Random();
                //byte Next() => (byte)rnd.Next(0, 256);
                //for (int i = 0; i < panel.RowDefinitions.Count; i++) {
                //    var g = new Grid { Background = new SolidColorBrush(MColor.FromRgb(Next(), Next(), Next())) };
                //    Grid.SetColumn(g, 1);
                //    Grid.SetRow(g, i);
                //    panel.Children.Add(g);
                //}
            }

            TextBlock title; if (showCaption) {
                title = new() {
                    FontSize = captionfontsize,
                    Text = caption,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextDecorations = TextDecorations.Underline,
                    TextAlignment = TextAlignment.Center
                };
                Grid.SetColumn(title, 1);
                Grid.SetRow(title, 2);
                panel.Children.Add(title);
            }

            TextBlock footer; if (showLink) {
                footer = new() {
                    FontSize = footerfontsize,
                    Text = link.Replace("?", "\n?"),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center
                };
                Grid.SetColumn(footer, 1);
                Grid.SetRow(footer, 4);
                panel.Children.Add(footer);
            }

            Image img; {
                img = new Image() {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Source = qrCodeBitmap.ToBitmapSource()
                };
                Grid.SetColumn(img, 1);
                Grid.SetRow(img, 3);
                panel.Children.Add(img);
            }

            Grid logoGrid; {
                logoGrid = new() {
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Height = logoheight
                };
                Grid.SetRow(logoGrid, 1);
                Grid.SetColumn(logoGrid, 1);
                panel.Children.Add(logoGrid);
            }

            SvgViewbox logoBox; {
                logoBox = new() {
                    Source = new("pack://application:,,,/BarCodeGenerator;component/logo_black.svg")
                };
                logoGrid.Children.Add(logoBox);
            }
            
            // measure, arrange and then print
            {
                panel.Measure(new(width, height));
                panel.Arrange(new(new(0, 0), panel.DesiredSize));
                dlg.PrintVisual(panel, "Casambi QR Code Generator");
            }
        }
    }
}
