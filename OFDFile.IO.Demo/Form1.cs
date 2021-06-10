using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OFDFile.IO.Demo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void textBox1_MouseClick(object sender, MouseEventArgs e)
        {
            openFileDialog1.ShowDialog();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            string fileName = openFileDialog1.FileName;
            textBox1.Text = fileName;

            try
            {
                var reader = new OFDFileFastReader();
                var fileInfo = reader.ReadFile(fileName);
                var rowDatas = fileInfo.RawDatas.Select(x => OFDFileFastReader.DeserilizeRowData2(fileInfo.FieldInfos, x)).ToList();
                dataGridView1.SuspendLayout();
                dataGridView1.Rows.Clear();
                dataGridView1.Columns.Clear();
                dataGridView1.ColumnCount = fileInfo.FieldInfos.Count;
                for (int i = 0; i < fileInfo.FieldInfos.Count; i++)
                {
                    var col = dataGridView1.Columns[i];
                    col.Name = fileInfo.FieldInfos[i].FieldName;
                    col.ValueType = fileInfo.FieldInfos[i].FiledDataType;
                }
                foreach (var row in rowDatas)
                {
                    dataGridView1.Rows.Add(row);
                }
                dataGridView1.ResumeLayout();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

        }
    }
}
