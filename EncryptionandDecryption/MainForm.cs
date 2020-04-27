using DevExpress.ClipboardSource.SpreadsheetML;
using DevExpress.Utils.Extensions;
using DevExpress.Utils.Internal;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WINDecrypt
{
    public partial class FormMainForm : XtraForm
    {
        /// <summary>
        /// 加密文件输出路径
        /// </summary>
        public string EncryptOutPath { get; set; }
        public string DecryptOutPath { get; set; }

        public string EPWD { get; set; }
        public string DPWD { get; set; }

        public DataTable dtEncrypt { get; set; }
        public DataTable dtDecrypt { get; set; }
        public FormMainForm()
        {
            InitializeComponent();
            this.Load += Form1_Load;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            dtEncrypt = new DataTable();
            dtEncrypt.Columns.AddRange(new DataColumn[] {
                new DataColumn("FileName"),
                new DataColumn("Name"),
                new DataColumn("OutPath"),
                new DataColumn("Pwd"),
                new DataColumn("Progress"),
                new DataColumn("OutName"),
                new DataColumn("OutExt")
            });
            dtDecrypt = new DataTable();
            dtDecrypt.Columns.AddRange(new DataColumn[] {
                new DataColumn("DFileName"),
                new DataColumn("DName"),
                new DataColumn("DOutPath"),
                new DataColumn("DPwd"),
                new DataColumn("DProgress"),
                new DataColumn("DOutName"),
                new DataColumn("DOutExt")
            });
            gridControlEncrypt.DataSource = dtEncrypt;
            gridControlDecrypt.DataSource = dtDecrypt;
            txtPwd.DataBindings.Add("Text", this, "EPWD", true, DataSourceUpdateMode.OnPropertyChanged);
            txtDecPwd.DataBindings.Add("Text", this, "DPWD", true, DataSourceUpdateMode.OnPropertyChanged);
            this.repositoryItemButtonEditSetOutpath.ButtonClick += RepositoryItemButtonEditSetOutpath_ButtonClick;
            this.repositoryItemButtonEditRemove.ButtonClick += RepositoryItemButtonEditRemove_ButtonClick;
            this.repositoryItemButtonEditDRemove.ButtonClick += RepositoryItemButtonEditDRemove_ButtonClick;
            this.repositoryItemButtonEditDOutPath.ButtonClick += RepositoryItemButtonEditDOutPath_ButtonClick;
            this.repositoryItemButtonEditDecrypt.ButtonClick += RepositoryItemButtonEditDecrypt_ButtonClick;
            this.repositoryItemButtonEditEncrpt.ButtonClick += RepositoryItemButtonEditEncrpt_ButtonClick;
        }



        /// <summary>
        /// 单项加密
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RepositoryItemButtonEditEncrpt_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            var row = (gridControlEncrypt.DefaultView as GridView).GetFocusedDataRow();
            if (row != null)
            {
                string dest = Path.Combine(row["OutPath"].ToString(), row["OutName"].ToString() + "." + row["OutExt"].ToString());
                var res = Operating(row["FileName"].ToString(), dest, row["Pwd"].ToString(), true);
            }
        }

        /// <summary>
        /// 单项解密
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RepositoryItemButtonEditDecrypt_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            var row = (gridControlDecrypt.DefaultView as GridView).GetFocusedDataRow();
            if (row != null)
            {
                string dest = Path.Combine(row["DOutPath"].ToString(), row["DOutName"].ToString() + "." + row["DOutExt"].ToString());
                var res = Operating(row["DFileName"].ToString(), dest, row["DPwd"].ToString(), false);
            }
        }

        /// <summary>
        /// 设置单项解密路径
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RepositoryItemButtonEditDOutPath_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            using (FolderBrowserDialog folderBrowser = new FolderBrowserDialog())
            {
                if (folderBrowser.ShowDialog() == DialogResult.OK)
                {
                    var row = (gridControlDecrypt.DefaultView as DevExpress.XtraGrid.Views.Grid.GridView).GetFocusedDataRow();
                    row["DOutPath"] = folderBrowser.SelectedPath;
                }
                gridControlEncrypt.RefreshDataSource();
            }
        }

        /// <summary>
        /// 解密项目移除
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RepositoryItemButtonEditDRemove_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            var row = (gridControlDecrypt.DefaultView as GridView).GetFocusedDataRow();
            if (row != null)
            {
                int.TryParse(row["DProgress"].ToString(), out int cur_progress);
                if (cur_progress == 100 || cur_progress < 1)
                {
                    dtDecrypt.Rows.Remove(row);
                    gridControlDecrypt.RefreshDataSource();
                }
            }
        }

        /// <summary>
        /// 加密项目移除
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RepositoryItemButtonEditRemove_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            var row = (gridControlEncrypt.DefaultView as GridView).GetFocusedDataRow();
            if (row != null)
            {
                int.TryParse(row["Progress"].ToString(), out int cur_progress);
                if (cur_progress == 100 || cur_progress < 1)
                {
                    dtEncrypt.Rows.Remove(row);
                    gridControlEncrypt.RefreshDataSource();
                }
            }
        }

        /// <summary>
        /// 加密单项输出路径设置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RepositoryItemButtonEditSetOutpath_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            using (FolderBrowserDialog folderBrowser = new FolderBrowserDialog())
            {
                if (folderBrowser.ShowDialog() == DialogResult.OK)
                {
                    var row = (gridControlEncrypt.DefaultView as DevExpress.XtraGrid.Views.Grid.GridView).GetFocusedDataRow();
                    row["OutPath"] = folderBrowser.SelectedPath;
                }
                gridControlEncrypt.RefreshDataSource();
            }
        }

        /// <summary>
        /// 选择总输出路径
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonEditFolder_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            using (FolderBrowserDialog folderBrowser = new FolderBrowserDialog())
            {
                if (folderBrowser.ShowDialog() == DialogResult.OK)
                {
                    buttonEditFolder.Text = EncryptOutPath = folderBrowser.SelectedPath;
                }
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog file = new OpenFileDialog())
            {
                file.Multiselect = true;
                if (file.ShowDialog() == DialogResult.OK)
                {
                    string[] files = file.FileNames;
                    if (files?.Length > 0)
                    {
                        foreach (var item in files)
                        {
                            var row = dtEncrypt.NewRow();
                            row["FileName"] = item;
                            row["Name"] = Path.GetFileName(item);
                            row["OutPath"] = EncryptOutPath;
                            row["Pwd"] = EPWD;
                            row["OutName"] = Path.GetFileNameWithoutExtension(item);
                            row["OutExt"] = "rj";
                            row["Progress"] = 0;
                            dtEncrypt.Rows.Add(row);
                        }
                    }
                }
            }
        }

        private void BtnEncrypt_Click(object sender, EventArgs e)
        {
            if (dtEncrypt?.Rows.Count > 0)
            {
                Parallel.ForEach(dtEncrypt.Rows.Cast<DataRow>(), new Action<DataRow>(dr =>
                {
                    if (dr != null)
                    {
                        string dest = Path.Combine(dr["OutPath"].ToString(), Path.GetFileNameWithoutExtension(dr["OutName"].ToString()) + "." + dr["OutExt"].ToString());
                        var res = Operating(dr["FileName"].ToString(), dest, dr["Pwd"].ToString(), true);
                    }
                }));
            }
        }

        /// <summary>
        /// 加密解密 报告进度
        /// </summary>
        /// <param name="inFile"></param>
        /// <param name="outFile"></param>
        /// <param name="type">ture:加密  false:解密</param>
        /// <returns></returns>
        public async Task Operating(string inFile, string outFile, string pwd, bool type)
        {
            CancellationTokenSource token = new CancellationTokenSource();
            var progress = new Progress<double>();
            progress.ProgressChanged += new EventHandler<double>((o, d) =>
            {
                if (type)
                {
                    DataRow[] rows = dtEncrypt.Select("Name='" + Path.GetFileName(inFile) + "'");
                    if (rows?.Length > 0)
                    {
                        this.Invoke(new Action(() =>
                        {
                            if (rows[0] != null)
                            {
                                rows[0]["Progress"] = (int)d;
                                gridControlEncrypt.RefreshDataSource();
                            }
                        }));
                    }
                }
                else
                {
                    DataRow[] rows = dtDecrypt.Select("DName='" + Path.GetFileName(inFile) + "'");
                    if (rows?.Length > 0)
                    {
                        this.Invoke(new Action(() =>
                        {
                            if (rows[0] != null)
                            {
                                rows[0]["DProgress"] = (int)d;
                                gridControlDecrypt.RefreshDataSource();
                            }
                        }));
                    }
                }
            });
            if (type)
            {
                await EncryptOperation.EncryptFile(inFile, outFile, pwd, progress, token);
            }
            else
            {
                await EncryptOperation.DecryptFile(inFile, outFile, pwd, progress, token);
            }
        }

        private void BtnAddDecrypt_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog file = new OpenFileDialog())
            {
                file.Multiselect = true;
                if (file.ShowDialog() == DialogResult.OK)
                {
                    string[] files = file.FileNames;
                    if (files?.Length > 0)
                    {
                        foreach (var item in files)
                        {
                            var row = dtDecrypt.NewRow();
                            row["DFileName"] = item;
                            row["DName"] = Path.GetFileName(item);
                            row["DOutPath"] = DecryptOutPath;
                            row["DPwd"] = DPWD;
                            row["DOutName"] = Path.GetFileNameWithoutExtension(item);
                            row["DOutExt"] = "";
                            row["DProgress"] = 0;
                            dtDecrypt.Rows.Add(row);
                        }
                    }
                }
            }
        }

        private void BtnDecrypt_Click(object sender, EventArgs e)
        {
            if (dtDecrypt?.Rows.Count > 0)
            {
                Parallel.ForEach(dtDecrypt.Rows.Cast<DataRow>(), new Action<DataRow>(async dr =>
                {
                    if (dr != null)
                    {
                        string dest = Path.Combine(dr["DOutPath"].ToString(), Path.GetFileNameWithoutExtension(dr["DOutName"].ToString()) + "." + dr["DOutExt"].ToString());
                        await Operating(dr["DFileName"].ToString(), dest, dr["DPwd"].ToString(), false);
                    }
                }));
            }
        }

        /// <summary>
        /// 解密输出目录
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonEdit1_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            using (FolderBrowserDialog folderBrowser = new FolderBrowserDialog())
            {
                if (folderBrowser.ShowDialog() == DialogResult.OK)
                {
                    buttonEdit1.Text = DecryptOutPath = folderBrowser.SelectedPath;
                }
            }
        }
    }
}
