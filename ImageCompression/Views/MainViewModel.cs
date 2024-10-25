﻿using ImageCompression.Models;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Encoder = System.Drawing.Imaging.Encoder;

namespace ImageCompression.Views
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ImageModel Image { get; set; }
        public ICommand SelectImageCommand { get; set; }
        public ICommand CompressImageCommand { get; set; }

        public MainViewModel()
        {
            Image = new ImageModel();
            SelectImageCommand = new RelayCommand(SelectImage);
            CompressImageCommand = new RelayCommand(CompressImage);
        }

        private void SelectImage(object parameter)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "All supported graphics|*.jpg;*.jpeg;*.png;*.bmp;*.gif|JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|Portable Network Graphic (*.png)|*.png|Bitmap (*.bmp)|*.bmp|Graphics Interchange Format (*.gif)|*.gif"
            };

            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                Image.FilePath = openFileDialog.FileName;
                OnPropertyChanged(nameof(Image.FilePath));
            }
        }


        private void CompressImage(object parameter)
        {
            var type = Image.CompressionType.Split(" ").Last();
            string outputFileName = $"{Path.GetFileNameWithoutExtension(Image.FilePath)}_{type}_{DateTime.Now:yyyyMMdd_HHmmss}{Path.GetExtension(Image.FilePath)}";
            Image.OutputPath = Path.Combine(Path.GetDirectoryName(Image.FilePath), outputFileName);

            switch (type)
            {
                case "JPEG":
                    CompressJpeg(Image.FilePath, Image.OutputPath, Image.Quality, Image.Progressive);
                    break;
                case "WebP":
                    CompressWebP(Image.FilePath, Image.OutputPath, Image.Quality);
                    break;
                case "BMP":
                    File.Copy(Image.FilePath, Image.OutputPath, true);
                    break;
                case "GIF":
                    File.Copy(Image.FilePath, Image.OutputPath, true);
                    break;
            }
        }

        private void CompressWithFFmpeg(string inputPath, string outputPath, int quality)
        {
            var ffmpeg = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i {inputPath} -q:v {quality} -progressive 1 {outputPath}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (var process = Process.Start(ffmpeg))
            {
                process.WaitForExit();
            }
        }

        private void CompressJpeg(string inputPath, string outputPath, int quality, bool progressive)
        {
            using (var bitmap = new Bitmap(inputPath))
            {
                var encoder = GetEncoder(ImageFormat.Jpeg);
                if (encoder != null)
                {
                    var encoderParameters = new EncoderParameters(1);
                    encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);
                    bitmap.Save(outputPath, encoder, encoderParameters);
                }
            }

            if (progressive)
            {
                CompressWithFFmpeg(inputPath, outputPath, quality);
            }
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageDecoders();
            foreach (var codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        private void CompressWebP(string inputPath, string outputPath, int quality)
        {
            using (var input = File.OpenRead(inputPath))
            using (var bitmap = SKBitmap.Decode(input))
            using (var image = SKImage.FromBitmap(bitmap))
            using (var data = image.Encode(SKEncodedImageFormat.Webp, quality))
            using (var output = File.OpenWrite(outputPath))
            {
                data.SaveTo(output);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}