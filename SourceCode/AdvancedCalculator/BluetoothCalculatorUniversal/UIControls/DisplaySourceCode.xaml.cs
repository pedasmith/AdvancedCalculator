using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Shipwreck.UIControls
{
    public sealed partial class DisplaySourceCode : UserControl
    {
        public DisplaySourceCode()
        {
            this.InitializeComponent();
            Features.Init();  // Might not be initialized yet if the purchase screen is the first screen.
            SetPaid();
            Features.PaidChanged += SetPaid;
        }

        private async Task<StorageFolder> GetSourceFolderAsync()
        {
            StorageFolder InstallationFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            var assets = await InstallationFolder.GetFolderAsync("Assets");
            var codeFiles = await assets.GetFolderAsync("SourceCode");
            return codeFiles;
        }

        private void AddFile(StorageFile file)
        {
            var button = new Button()
            {
                Content = file.Name,
                Tag = file.Name,
                MinWidth = 200,
            };
            button.Click += OnShowCode;
            uiFilenameList.Children.Add (button);
        }

        public async Task InitFileList(List<string> priority)
        {
            var codeFiles = await GetSourceFolderAsync();
            var files = await codeFiles.GetFilesAsync();
            string firstFile = null;
            foreach (var name in priority)
            {
                var file = files.FirstOrDefault(x => x.Name == name);
                if (file != null)
                {
                    AddFile(file);
                    if (firstFile == null) firstFile = file.Name;
                }
            }
            foreach (var file in files)
            {
                var dup = priority.FirstOrDefault(x => x == file.Name);
                if (dup == null)
                {
                    AddFile(file);
                    if (firstFile == null) firstFile = file.Name;
                }
            }
            if (firstFile != null)
            {
                await DoShowCodeAsync(firstFile);
            }
        }

        private async void OnShowCode(object sender, RoutedEventArgs args)
        {
            var name = (sender as Button).Tag as string;
            await DoShowCodeAsync(name);
        }

        string lastFilename = null;
        private async Task DoShowCodeAsync(string name)
        {
            lastFilename = name;
            var codeFiles = await GetSourceFolderAsync();
            var file = await codeFiles.GetFileAsync (name);
            string contents = string.Format("Unable to read file {0}.  You can still Save As to save the file.", name);
            try
            {
                contents = await FileIO.ReadTextAsync(file);
            }
            catch (Exception)
            {
            }
            uiTextContentA.Text = contents;
            uiTextContentB.Text = contents;
            uiFileName.Text = name;

        }

        void SetPaid()
        {
            var paid = Features.HasSourceCode;
            var Visible = Visibility.Visible;
            var Collapsed = Visibility.Collapsed;
            uiTextContentA.Visibility = paid ? Visible : Collapsed;
            uiTextContentB.Visibility = paid ? Collapsed : Visible;
            uiHidingRectangle.Visibility = paid ? Collapsed : Visible;
            uiPaidOnly.Visibility = paid ? Collapsed : Visible;
        }


        private async void OnCopySourceCode(object sender, RoutedEventArgs args)
        {
            var paid = Features.HasSourceCode;
            if (!paid)
            {
                await Messages.PopupCantCopySourceCodeToClipboardAsync();
                return;
            }
            var str = uiTextContentA.Text;

            var dp = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dp.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            dp.SetText(str);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dp);
        }

        private async void OnSaveSourceCode(object sender, RoutedEventArgs args)
        {
            var paid = Features.HasSourceCode;
            if (!paid)
            {
                await Messages.PopupCantSaveAsSourceCodeAsync();
                return;
            }

            var name = lastFilename;
            var lastDot = name.LastIndexOf('.');
            var ext = (lastDot < 0) ? name : name.Substring(lastDot);
            var codeFiles = await GetSourceFolderAsync();
            var file = await codeFiles.GetFileAsync(name);

            var picker = new Windows.Storage.Pickers.FileSavePicker();
            picker.SuggestedFileName = name;
            picker.FileTypeChoices.Add("File", new List<string>() { ext });
            var result = await picker.PickSaveFileAsync();
            if (result == null)
            {
                await Messages.PopupSaveAsNoFileSpecified();
                return;
            }
            using (var writeStream = await result.OpenStreamForWriteAsync())
            {
                var readStream = await file.OpenStreamForReadAsync();
                var buffer = new byte[16 * 10242];
                await readStream.CopyToAsync(writeStream);
            }
        }
    }
}
