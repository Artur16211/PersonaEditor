﻿using PersonaEditorLib;
using PersonaEditorLib.Text;
using AuxiliaryLibraries.WPF;
using Microsoft.Win32;
using PersonaEditor.Classes;
using PersonaEditor.Controls.ToolBox;
using PersonaEditorCMD;
using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using PersonaEditorLib.Other;
using System.Collections.Generic;

namespace PersonaEditor.ViewModels
{
    public partial class TreeViewItemVM : BindingObject
    {
        private void UpdateContextMenu()
        {
            _contextMenu.Clear();

            MenuItem menuItem = null;
            if (PersonaFileHelper.IsEdited(PersonaFile))
            {
                menuItem = new MenuItem();
                menuItem.Header = Application.Current.Resources.MergedDictionaries.GetString("tree_Edit");
                menuItem.Command = new RelayCommand(ContextMenu_Edit);
                _contextMenu.Add(menuItem);
            }

            menuItem = new MenuItem();
            menuItem.Header = Application.Current.Resources.MergedDictionaries.GetString("tree_Replace");
            menuItem.Command = new RelayCommand(ContextMenu_Replace);
            _contextMenu.Add(menuItem);

            _contextMenu.Add(new Separator());

            menuItem = new MenuItem();
            menuItem.Header = Application.Current.Resources.MergedDictionaries.GetString("tree_SaveAs");
            menuItem.Command = new RelayCommand(ContextMenu_SaveAs);
            _contextMenu.Add(menuItem);

            if (PersonaFileHelper.HaveSubFiles(PersonaFile))
            {
                menuItem = new MenuItem();
                menuItem.Header = Application.Current.Resources.MergedDictionaries.GetString("tree_SaveAll");
                menuItem.Command = new RelayCommand(ContextMenu_SaveAll);
                _contextMenu.Add(menuItem);

                menuItem = new MenuItem();
                menuItem.Header = Application.Current.Resources.MergedDictionaries.GetString("tree_Add");
                menuItem.Command = new RelayCommand(ContextMenu_Add);
                _contextMenu.Add(menuItem);

                menuItem = new MenuItem();
                menuItem.Header = Application.Current.Resources.MergedDictionaries.GetString("tree_AddFolder");
                menuItem.Command = new RelayCommand(ContextMenu_AddFolder);
                _contextMenu.Add(menuItem);
            }
        }

        private void ContextMenu_Edit()
        {
            ItemAction?.Invoke(this, UserTreeViewItemEventEnum.Open);
        }

        private void ContextMenu_Replace()
        {
            FormatEnum fileType = PersonaFile.GameData.Type;

            OpenFileDialog OFD = new OpenFileDialog();
            string name = PersonaFile.Name.Replace('/', '+');
            OFD.Filter = $"RAW(*{Path.GetExtension(name)})|*{Path.GetExtension(name)}";
            OFD.CheckFileExists = true;
            OFD.CheckPathExists = true;
            OFD.Multiselect = false;

            if (PersonaFile.GameData is IImage)
                OFD.Filter += $"|PNG (*.png)|*.png";
            if (PersonaFile.GameData is ITable)
                OFD.Filter += $"|XML data table (*.xml)|*.xml";
            if (PersonaFile.GameData is BMD)
                OFD.Filter += $"|Persona Text Project (*.ptp)|*.ptp";

            if (OFD.ShowDialog() == true)
            {
                if (OFD.FilterIndex == 1)
                {
                    var item = GameFormatHelper.OpenFile(PersonaFile.Name, File.ReadAllBytes(OFD.FileName), fileType);

                    if (item != null)
                        PersonaFile.GameData = item.GameData;
                }
                else
                {
                    string ext = Path.GetExtension(OFD.FileName);
                    if (ext.Equals(".png", StringComparison.CurrentCultureIgnoreCase))
                        PersonaEditorTools.OpenImageFile(PersonaFile, OFD.FileName);
                    else if (ext.Equals(".xml", StringComparison.CurrentCultureIgnoreCase))
                        PersonaEditorTools.OpenTableFile(PersonaFile, OFD.FileName);
                    else if (ext.Equals(".ptp", StringComparison.CurrentCultureIgnoreCase))
                    {
                        var result = ToolBox.Show(ToolBoxType.OpenPTP);
                        if (result == ToolBoxResult.Ok)
                            PersonaEditorTools.OpenPTPFile(PersonaFile, OFD.FileName, Static.EncodingManager.GetPersonaEncoding(ApplicationSettings.AppSetting.Default.OpenPTP_Font));
                    }
                    else
                        throw new Exception("OpenPersonaFileDialog");
                }

                Update(_personaFile);
                if (_isSelected)
                    ItemAction?.Invoke(this, UserTreeViewItemEventEnum.Selected);
            }
        }

        private void ContextMenu_SaveAs()
        {
            SaveFileDialog SFD = new SaveFileDialog();
            SFD.OverwritePrompt = true;
            SFD.AddExtension = false;
            SFD.FileName = PersonaFile.Name.Replace('/', '+');
            SFD.Filter = $"RAW(*{Path.GetExtension(SFD.FileName)})|*{Path.GetExtension(SFD.FileName)}";

            if (PersonaFile.GameData is IImage)
                SFD.Filter += $"|PNG (*.png)|*.png";
            if (PersonaFile.GameData is ITable)
                SFD.Filter += $"|XML data table (*.xml)|*.xml";
            if (PersonaFile.GameData is BMD)
                SFD.Filter += $"|Persona Text Project (*.ptp)|*.ptp";
            if (PersonaFile.GameData is PTP)
                SFD.Filter += $"|BMD Text File (*.bmd)|*.bmd";

            SFD.InitialDirectory = Path.GetDirectoryName(Static.OpenedFile);
            if (SFD.ShowDialog() == true)
                if (SFD.FilterIndex == 1)
                    File.WriteAllBytes(SFD.FileName, PersonaFile.GameData.GetData());
                else
                {
                    string ext = Path.GetExtension(SFD.FileName);
                    if (ext.Equals(".png", StringComparison.CurrentCultureIgnoreCase))
                        PersonaEditorCMD.PersonaEditorTools.SaveImageFile(PersonaFile, SFD.FileName);
                    else if (ext.Equals(".ptp", StringComparison.CurrentCultureIgnoreCase))
                    {
                        var result = ToolBox.Show(ToolBoxType.SaveAsPTP);
                        if (result == ToolBoxResult.Ok)
                        {
                            PersonaEncoding temp = ApplicationSettings.AppSetting.Default.SaveAsPTP_CO2N ? Static.EncodingManager.GetPersonaEncoding(ApplicationSettings.AppSetting.Default.SaveAsPTP_Font) : null;
                            PersonaEditorCMD.PersonaEditorTools.SavePTPFile(PersonaFile, SFD.FileName, temp);
                        }
                    }
                    else if (ext.Equals(".bmd", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Encoding encoding = Static.EncodingManager.GetPersonaEncoding(ApplicationSettings.AppSetting.Default.PTPNewDefault);
                        BMD bmd = new BMD(PersonaFile.GameData as PTP, encoding);
                        File.WriteAllBytes(SFD.FileName, bmd.GetData());
                    }
                    else if (ext.Equals(".xml", StringComparison.CurrentCultureIgnoreCase))
                        PersonaEditorCMD.PersonaEditorTools.SaveTableFile(PersonaFile, SFD.FileName);
                    else throw new Exception("SavePersonaFileDialog");
                }
        }

        private void ContextMenu_SaveAll()
        {
            var FBD = new CommonOpenFileDialog();
            FBD.IsFolderPicker = true;
            FBD.Title = "Export All Files to Directory";
            if (FBD.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string path = FBD.FileName;

                foreach (var item in PersonaFile.GameData.SubFiles)
                    File.WriteAllBytes(Path.Combine(path, item.Name), item.GameData.GetData());
            }
        }

        private void ContextMenu_Add()
        {
            var FBD = new CommonOpenFileDialog();
            FBD.Title = "Select file to add to archive";
            if (FBD.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string path = FBD.FileName;

                var fileName = Path.GetFileName(path);
                byte[] fileBytes = File.ReadAllBytes(path);
                var gameFileObj = GameFormatHelper.OpenFile(fileName, fileBytes);
                PersonaFile.GameData.SubFiles.Add(gameFileObj);

                Update(_personaFile);
            }
        }

        private void ContextMenu_AddFolder()
        {
            var FBD = new CommonOpenFileDialog();
            FBD.Title = "Select Folder to add items from";
            FBD.IsFolderPicker = true;
            FBD.Multiselect = true;
            if (FBD.ShowDialog() == CommonFileDialogResult.Ok)
            {
                IEnumerable<string> paths = FBD.FileNames;

                foreach (var path in paths)
                {
                    foreach (var file in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories))
                    {
                        var fileName = Utilities.GetRelativePath(file, $"{Path.GetDirectoryName(path)}\\");
                        byte[] fileBytes = File.ReadAllBytes(file);
                        var gameFileObj = GameFormatHelper.OpenFile(fileName, fileBytes);

                        PersonaFile.GameData.SubFiles.Add(gameFileObj);
                    }
                }

                Update(_personaFile);
            }
        }
    }
}