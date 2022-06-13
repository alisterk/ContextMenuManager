﻿using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using System;
using System.IO;
using System.Windows.Forms;

namespace ContextMenuManager.Controls
{
    sealed class NewShellDialog : CommonDialog
    {
        public string ShellPath { get; set; }//传入的Shell注册表路径
        public string ScenePath { get; set; }//菜单项所处环境注册表路径
        public string NewItemRegPath { get; private set; }//返回的新ShellItem的注册表路径
        public string NewItemKeyName => RegistryEx.GetKeyName(NewItemRegPath);

        public override void Reset() { }

        protected override bool RunDialog(IntPtr hwndOwner)
        {
            using(NewShellForm frm = new NewShellForm
            {
                ScenePath = this.ScenePath,
                ShellPath = this.ShellPath
            })
            {
                frm.TopMost = AppConfig.TopMost;
                bool flag = frm.ShowDialog() == DialogResult.OK;
                if(flag) this.NewItemRegPath = frm.NewItemRegPath;
                return flag;
            }
        }

        sealed class NewShellForm : NewItemForm
        {
            public string ShellPath { get; set; }
            public string NewItemRegPath { get; private set; }//返回的新建菜单项注册表路径
            public string ScenePath { get; set; }//菜单所处环境路径，用于判断添加后缀

            readonly RadioButton rdoSingle = new RadioButton
            {
                Text = AppString.Dialog.SingleMenu,
                AutoSize = true,
                Checked = true
            };
            readonly RadioButton rdoMulti = new RadioButton
            {
                Text = AppString.Dialog.MultiMenu,
                AutoSize = true
            };
            readonly ShellExecuteCheckBox chkSE = new ShellExecuteCheckBox();

            static readonly string[] DirScenePaths = {
                ShellList.MENUPATH_DIRECTORY,
                ShellList.MENUPATH_BACKGROUND,
                $@"{ShellList.SYSFILEASSPATH}\Directory."
            };
            static readonly string[] FileObjectsScenePaths = {
                ShellList.MENUPATH_FILE,
                ShellList.MENUPATH_FOLDER,
                ShellList.MENUPATH_ALLOBJECTS,
                ShellList.SYSFILEASSPATH,
                ShellList.MENUPATH_UNKNOWN,
                ShellList.MENUPATH_UWPLNK
            };

            protected override void InitializeComponents()
            {
                base.InitializeComponents();
                this.Controls.AddRange(new Control[] { rdoSingle, rdoMulti, chkSE });
                rdoSingle.Top = rdoMulti.Top = chkSE.Top = btnOK.Top + (btnOK.Height - rdoSingle.Height) / 2;
                rdoSingle.Left = lblCommand.Left;
                rdoMulti.Left = rdoSingle.Right + 20.DpiZoom();
                chkSE.Left = rdoMulti.Right + 20.DpiZoom();

                rdoMulti.CheckedChanged += (sender, e) =>
                {
                    if(rdoMulti.Checked)
                    {
                        chkSE.Checked = false;
                        if(WinOsVersion.Current == WinOsVersion.Vista)
                        {
                            AppMessageBox.Show(AppString.Message.VistaUnsupportedMulti);
                            rdoSingle.Checked = true;
                            return;
                        }
                    }
                    lblCommand.Enabled = txtFilePath.Enabled = lblArguments.Enabled
                    = txtArguments.Enabled = btnBrowse.Enabled = chkSE.Enabled = !rdoMulti.Checked;
                };

                btnBrowse.Click += (sender, e) => BrowseFile();

                btnOK.Click += (sender, e) =>
                {
                    if(txtText.Text.IsNullOrWhiteSpace())
                    {
                        AppMessageBox.Show(AppString.Message.TextCannotBeEmpty);
                    }
                    else
                    {
                        AddNewItem();
                        DialogResult = DialogResult.OK;
                    }
                };
            }

            private void BrowseFile()
            {
                using(OpenFileDialog dlg = new OpenFileDialog())
                {
                    dlg.DereferenceLinks = false;
                    dlg.Filter = $"{AppString.Dialog.Program}|*.exe|{AppString.Dialog.AllFiles}|*";
                    if(dlg.ShowDialog() != DialogResult.OK) return;
                    string filePath = dlg.FileName;
                    string arguments = "";
                    ItemText = Path.GetFileNameWithoutExtension(filePath);
                    string extension = Path.GetExtension(filePath).ToLower();
                    if(extension == ".lnk")
                    {
                        using(ShellLink shellLink = new ShellLink(filePath))
                        {
                            filePath = shellLink.TargetPath;
                            arguments = shellLink.Arguments;
                            extension = Path.GetExtension(filePath);
                        }
                    }
                    string exePath = FileExtension.GetExtentionInfo(FileExtension.AssocStr.Executable, extension);
                    if(File.Exists(exePath))
                    {
                        ItemFilePath = exePath;
                        Arguments = filePath;
                        if(!arguments.IsNullOrWhiteSpace()) Arguments += " " + arguments;
                    }
                    else
                    {
                        ItemFilePath = filePath;
                        Arguments = arguments;
                    }
                    if(Array.FindIndex(DirScenePaths, path
                       => ScenePath.StartsWith(path, StringComparison.OrdinalIgnoreCase)) != -1)
                    {
                        if(ScenePath != ShellList.MENUPATH_BACKGROUND)
                        {
                            if(!Arguments.IsNullOrWhiteSpace()) Arguments += " ";
                            Arguments += "\"%V\"";//自动加目录后缀
                        }
                    }
                    else if(Array.FindIndex(FileObjectsScenePaths, path
                       => ScenePath.StartsWith(path, StringComparison.OrdinalIgnoreCase)) != -1)
                    {
                        if(!Arguments.IsNullOrWhiteSpace()) Arguments += " ";
                        Arguments += "\"%1\"";//自动加文件对象后缀
                    }
                }
            }

            private void AddNewItem()
            {
                using(var shellKey = RegistryEx.GetRegistryKey(ShellPath, true, true))
                {
                    string keyName = "Item";
                    NewItemRegPath = ObjectPath.GetNewPathWithIndex($@"{ShellPath}\{keyName}", ObjectPath.PathType.Registry, 0);
                    keyName = RegistryEx.GetKeyName(NewItemRegPath);

                    using(var key = shellKey.CreateSubKey(keyName, true))
                    {
                        key.SetValue("MUIVerb", ItemText);
                        if(rdoMulti.Checked)
                            key.SetValue("SubCommands", "");
                        else
                        {
                            if(!ItemCommand.IsNullOrWhiteSpace())
                            {
                                string command;
                                if(!chkSE.Checked) command = ItemCommand;
                                else command = ShellExecuteDialog.GetCommand(ItemFilePath, Arguments, chkSE.Verb, chkSE.WindowStyle);
                                key.CreateSubKey("command", true).SetValue("", command);
                            }
                        }
                    }
                }
            }
        }
    }
}