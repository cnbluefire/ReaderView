using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace ReaderView.Controls
{
    public sealed partial class ReaderPage : UserControl
    {
        public ReaderPage()
        {
            this.InitializeComponent();
        }

        FrameworkElement ContentControl;

        public void SetContent(string content,double lineHeight = 10)
        {
            if (string.IsNullOrEmpty(content)) return;
            var paragraphs = content.Replace("\r", string.Empty).Split('\n').Select(x =>
            {
                var run = new Run() { Text = x };
                var paragraph = new Paragraph();
                paragraph.Inlines.Add(run);
                return paragraph;
            });

            var textBlock = new RichTextBlock();

            foreach (var paragraph in paragraphs)
            {
                textBlock.Blocks.Add(paragraph);
            }

            textBlock.Margin = Padding;
            textBlock.TextWrapping = TextWrapping.Wrap;
            textBlock.IsTextSelectionEnabled = false;
            textBlock.LineHeight = lineHeight;

            this.Content = textBlock;
            ContentControl = textBlock;
        }

        public void SetContent(ReaderPage readerPage)
        {
            var textBlock = new RichTextBlockOverflow();

            if (readerPage.ContentControl is RichTextBlock rtb)
            {
                rtb.OverflowContentTarget = textBlock;
            }
            else if (readerPage.ContentControl is RichTextBlockOverflow of)
            {
                of.OverflowContentTarget = textBlock;
            }

            textBlock.Margin = Padding;

            this.Content = textBlock;
            ContentControl = textBlock;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (ContentControl != null)
            {
                ContentControl.Measure(new Size(availableSize.Width - Padding.Left - Padding.Right, availableSize.Height - Padding.Top - Padding.Bottom));
                if (ContentControl is RichTextBlock rtb)
                {
                    HasOverflow = rtb.HasOverflowContent;
                }
                else if (ContentControl is RichTextBlockOverflow of)
                {
                    HasOverflow = of.HasOverflowContent;
                }
            }
            return base.MeasureOverride(availableSize);
        }

        public bool HasOverflow { get; private set; }
    }

    public enum ReaderPageMode
    {
        RichTextBlock,
        Overflow
    }
}
