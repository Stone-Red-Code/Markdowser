using Avalonia.Media.Imaging;

using System.IO;

namespace Markdowser.ViewModels.Content;
internal class CommonImageContentViewModel(string title, Stream stream) : ContentViewModelBase(title)
{
    public Bitmap Image { get; set; } = Bitmap.DecodeToHeight(stream, 1080);
}
