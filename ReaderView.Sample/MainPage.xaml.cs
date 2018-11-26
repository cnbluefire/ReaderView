using ReaderView.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace ReaderView.Sample
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        string content = "";
        int now = 0;

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/test1.txt"));

            using (var reader = new StreamReader(await file.OpenStreamForReadAsync()))
            {
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                content = await reader.ReadToEndAsync();
            }

            readerView.SetContent(content);
        }

        private void ReaderView_PrevPageSelected(object sender, EventArgs e)
        {
            if (now != 1) return;

            readerView.SetContent(content,SetContentMode.Last);
            now = 0;
        }

        private void ReaderView_NextPageSelected(object sender, EventArgs e)
        {
            if (now != 0) return;

            readerView.SetContent(content, SetContentMode.First);
            now = 1;
        }
    }
}
