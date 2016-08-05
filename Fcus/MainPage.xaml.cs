﻿using Octokit;
using Octokit.Internal;
using SimpleHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Text;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Fcus
{
    public sealed partial class MainPage : Windows.UI.Xaml.Controls.Page
    {
        private bool isControlKeyPressed;
        public string content;
        public StorageFile documentFile = null;
        public string documentTitle = "untitled";
        static InMemoryCredentialStore credentials = new InMemoryCredentialStore(new Credentials("3dfc7ffacf6e47daa83acb235570ac510f417a85"));
        static GitHubClient github = new GitHubClient(new ProductHeaderValue("Fcus-About"), credentials);

        public MainPage()
        {
            this.InitializeComponent();

            var applicationView = ApplicationView.GetForCurrentView();
            var titleBar = applicationView.TitleBar;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveForegroundColor = Colors.Gray;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonForegroundColor = Colors.Gray;

            Windows.ApplicationModel.Core.CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;

            AboutRender();

            content = "";
            documentTitle = "untitled";
            documentFile = null;

            this.Loaded += delegate { this.Focus(FocusState.Programmatic); };
        }

        private async void AboutRender()
        {
            var aboutcontent = new AboutModel();
            var aboutmd = await github.Gist.Get("bbf6e5254c9c3eec451b3596351bc2d4");
            var html = await github.Miscellaneous.RenderRawMarkdown(aboutmd.Files["Fcus_about.md"].Content);
            aboutcontent.CurrentHtmlString = html;
            this.DataContext = aboutcontent;
        }

        private async void ScriptNotify(object sender, NotifyEventArgs e)
        {
            if (e.Value == "change")
            {
                OnCodeContentChanged();
            }
            else
            {
                await new MessageDialog(e.Value).ShowAsync();
            }
        }

        private async void OnCodeContentChanged()
        {
            content = await editor.InvokeScriptAsync("getmd", null);
        }
        public async void NewFile()
        {
            content = "";
            documentTitle = "untitled";
            documentFile = null;
            mdtitle.Text = documentTitle;
            await editor.InvokeScriptAsync("setContent", new string[] { content });
        }

        private async void OpenFile()
        {
            // Open a text file.
            FileOpenPicker open =
                new FileOpenPicker();
            open.SuggestedStartLocation =
                PickerLocationId.DocumentsLibrary;
            open.FileTypeFilter.Add(".md");
            open.FileTypeFilter.Add(".markdown");
            open.FileTypeFilter.Add(".text");
            open.FileTypeFilter.Add(".txt");
            open.FileTypeFilter.Add(".mmd");
            open.FileTypeFilter.Add(".mdown");

            StorageFile file = await open.PickSingleFileAsync();
            var buffer = await FileIO.ReadBufferAsync(file);
            Encoding FileEncoding = SimpleHelpers.FileEncoding.DetectFileEncoding(buffer.AsStream(), Encoding.UTF8);
            var reader = new StreamReader(buffer.AsStream(), FileEncoding);

            content = reader.ReadToEnd().Replace("\r\n", "\n");
            documentFile = file;
            documentTitle = file.Name;
            mdtitle.Text = documentTitle;
            await editor.InvokeScriptAsync("setContent", new string[] { content });
        }

        private async void SaveFile()
        {
            if (documentFile == null)
            {
                var picker = new FileSavePicker();
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                picker.FileTypeChoices.Add("Markdown File", new List<string>() { ".md", ".markdown", ".mmd", ".mdown" });
                picker.FileTypeChoices.Add("Text Document", new List<string>() { ".text", ".txt" }); picker.SuggestedFileName = documentTitle;
                StorageFile file = await picker.PickSaveFileAsync();
                if (file != null)
                {
                    SaveDoc2File(file);
                    documentFile = file;
                    documentTitle = file.Name;
                }
            }
            else
            {
                SaveDoc2File(documentFile);
            }
        }

        private async void SaveDoc2File(StorageFile file)
        {
            content = await editor.InvokeScriptAsync("getmd", null);
            var bytes = Encoding.UTF8.GetBytes(content);
            await FileIO.WriteBytesAsync(file, bytes);
        }
        private void Grid_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Control) isControlKeyPressed = true;
            else if (isControlKeyPressed)
            {
                switch (e.Key)
                {
                    case VirtualKey.N:NewFile();  break;
                    case VirtualKey.O: OpenFile(); break;
                    case VirtualKey.S: SaveFile(); break;
                    case VirtualKey.A:OpenAbout(); break;

                }
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenAbout();
        }

        private void OpenAbout()
        {
            about.Visibility = (about.Visibility == Visibility.Visible) ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    class AboutModel
    {
        public string CurrentHtmlString { get; set; }
    }
}
