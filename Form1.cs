using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using System.Linq;
using System.Diagnostics;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using Crypto;
using System.Threading;

namespace ТестКафедра
{
    public partial class Form1 : Form
    {
        string res_path = Path.Combine(Environment.CurrentDirectory, "res");
        string test_path = Path.Combine(Environment.CurrentDirectory, "tests");
        string img_path = Path.Combine(Environment.CurrentDirectory, "img");

        List<string> QW = new List<string>();
        List<List<string>> ANS = new List<List<string>>();
        List<int> RIGHT_AN = new List<int>();
        List<int> USER_AN = new List<int>();

        Stopwatch stopWatch = new Stopwatch();

        public void get_qw(string fname)
        {
            XDocument doc = XDocument.Parse(fname);
            var ids = doc.Descendants("q").Attributes("text").Select(x => x.Value);

            foreach (var id in ids)
            {
                QW.Add(id);
            }

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(fname);
            XmlNodeList _Node = xmlDoc.SelectNodes("/test/qw/q");

            for (int i = 0; i < _Node.Count; i++)
            {
                List<string> temp = new List<string>();
                foreach (XmlNode item in _Node[i].ChildNodes)
                {
                    temp.Add(item.InnerText);
                }
                ANS.Add(temp);
            }

            for (int i = 0; i < _Node.Count; i++)
            {
                for (int j = 0; j < _Node[i].ChildNodes.Count; j++)
                {
                    if (_Node[i].ChildNodes[j].Attributes["right"].Value == "yes")
                    {
                        RIGHT_AN.Add(j);
                        USER_AN.Add(-1);
                    }
                }
            }
        }

        public Form1()
        {
            InitializeComponent();
            try
            {
                string[] filename = Directory.GetFiles(test_path, "*.caf.bin").Select(file => Path.GetFileName(file)).ToArray();
                comboBox1.DataSource = filename;
            }
            catch (DirectoryNotFoundException)
            {
                MessageBox.Show("Файлы теста не найдены!");
            }

            if (!Directory.Exists(res_path))
            {
                Directory.CreateDirectory(res_path);
            }
        }

        private void con_del()
        {
            var buttons = tabControl1.SelectedTab.Controls.OfType<RadioButton>().ToList();
            for (int i = 0; i < buttons.Count(); i++)
            {
                tabControl1.SelectedTab.Controls.Remove(buttons[i]);
                buttons[i].Dispose();
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            if (comboBox1.Items.Count > 0)
            {
                if (textBox1.Text.Length < 3 || String.IsNullOrWhiteSpace(textBox1.Text))
                {
                    MessageBox.Show("Введите ФИО (не менее трех символов)!");
                    textBox1.Text = null;
                }
                else
                {
                    MessageBox.Show("Вам предстоит пройти тест. Нажмите ОК для продолжения", "Внимание");

                    byte[] ba = File.ReadAllBytes(Path.Combine(test_path, comboBox1.Text));
                    object ob = ConvertByteArrayToObject(ba);
                    string test_text = encr2.Decrypt(ob.ToString());
                    test_text = test_text.Replace("\\r\\n", Environment.NewLine);
                    get_qw(test_text);

                    comboBox1.Enabled = false;
                    textBox1.ReadOnly = true;
                    button2.Visible = true;
                    dataGridView1.Visible = true;

                    for (int i = 0; i < QW.Count; i++)
                    {
                        var index = dataGridView1.Rows.Add();
                        dataGridView1.Rows[index].Cells[0].Value = i + 1;

                        DataGridViewButtonColumn c = (DataGridViewButtonColumn)dataGridView1.Columns[0];
                        c.FlatStyle = FlatStyle.Popup;
                        c.DefaultCellStyle.ForeColor = Color.Azure;
                        c.DefaultCellStyle.BackColor = Color.Silver;
                    }

                    dataGridView1_CellContentClick(dataGridView1, new DataGridViewCellEventArgs(this.dataGridView1.CurrentCell.ColumnIndex, this.dataGridView1.CurrentRow.Index));
                    button1.Visible = false;
                    stopWatch.Start();
                }
            }
            else
            {
                MessageBox.Show("Файлы теста отсутствуют!");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            stopWatch.Stop();
            con_del();

            //nv - кол-во пройденных вопросов
            int nv = USER_AN.Where(x => x >= 0).ToList().Count();

            //n - кол-во правильных ответов
            int n = 0;
            for (int i = 0; i < USER_AN.Count; i++)
            {
                if (RIGHT_AN[i] == USER_AN[i])
                {
                    n++;
                }
            }

            string[] textArray1;
            textArray1 = new string[] { "Тестирование завершено.\nПройдено вопросов: ", nv.ToString(), " из ", QW.Count.ToString(), ". Правильных ответов: ", n.ToString(), ".\nПроцент правильных ответов: ", ((n * 100) / QW.Count).ToString(), ".\n"};           
            MessageBox.Show(string.Concat(textArray1));

            //-------------------------Запись итогов-----------------------------
            string[] textArray = new string[] { textBox1.Text, " ", Environment.MachineName, " ", DateTime.Now.ToString("dd.MM.yyyy HH-mm-ss") };
            string path = string.Concat(textArray);

            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}";

            string[] tokens = Path.GetFileNameWithoutExtension(comboBox1.Text).Split('.');
            string name = new string(tokens[0].ToArray());

            List<string> f = new List<string>
            {
                "Тест: " +name + "\r\n",
                "ФИО: " + textBox1.Text + "\r\n",
                "Вопросов пройдено: " + nv + " из " + QW.Count + "\r\n",
                "Правильных ответов: " + n + "\r\n",
                "Процент правильных ответов: " + ((n * 100) / QW.Count) + "\r\n",
                "Время прохождение теста: " + (elapsedTime) + "\r\n",
                Environment.MachineName
            };

            byte[] ba = ConvertObjectToByteArray(encr2.Encrypt(string.Concat(f)));
            try
            {
                File.WriteAllBytes(Path.Combine(res_path, path + ".res.bin"), ba);
            }
            catch (IOException ex)
            {
                MessageBox.Show(ex.Message);
            }
            //---------------------конец записи итогов--------------------------------
            button1.Text = "Начать тест";
            label1.Text = null;
            pictureBox11.Image = null;
            pictureBox11.Visible = false;
            button1.Enabled = true;
            button2.Visible = false;
            comboBox1.Enabled = true;
            textBox1.ReadOnly = false;
            textBox1.Text = "";
            dataGridView1.Rows.Clear();
            dataGridView1.Visible = false;
            button1.Visible = true;
            label3.Text = null;
            QW.Clear();
            ANS.Clear();
            USER_AN.Clear();
            RIGHT_AN.Clear();
            stopWatch.Reset();

            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private void radioButton1_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < QW.Count; i++)
            {
                RadioButton RadioButton = (RadioButton)Controls.Find("RadioButton" + i.ToString(), true).FirstOrDefault();
                if (((RadioButton)sender) == RadioButton)
                {
                    USER_AN[dataGridView1.CurrentCell.RowIndex] = i;
                }
            }

            dataGridView1.CurrentCell.Style.BackColor = Color.Black;
            if (dataGridView1.CurrentCell.RowIndex == dataGridView1.RowCount - 1)
            {
                dataGridView1.CurrentCell = dataGridView1.Rows[0].Cells[0];
            }
            else
            {
                dataGridView1.CurrentCell = dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex + 1].Cells[0];
            }

            Thread.Sleep(200);

            if (QW.Count == USER_AN.Where(x => x >= 0).ToList().Count())
            {
                label3.Text = "Вы ответили на все вопросы.\nЕсли у вас есть время, можете проверить свои ответы.\nЧтобы узнать результаты тестирования,\nнажмите на кнопку 'Завершить тестирование'.";
            }
            else
            {
                label3.Text = $"Количество отвеченных вопросов: {USER_AN.Where(x => x >= 0).ToList().Count()}\nКоличество неотвеченных вопросов: {QW.Count - USER_AN.Where(x => x >= 0).ToList().Count()}";
            }
            dataGridView1_CellContentClick(dataGridView1, new DataGridViewCellEventArgs(dataGridView1.CurrentCell.ColumnIndex, dataGridView1.CurrentRow.Index));
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex == 1)
            {
                List<Tuple<string, DateTime>> Result = new List<Tuple<string, DateTime>> { };
                string[] files = Directory.GetFiles(res_path, "*.res.bin").Select(file => Path.GetFileName(file)).ToArray();

                foreach (string file in files)
                {
                    Result.Add(Tuple.Create(file, File.GetLastWriteTime(Path.Combine(res_path, file))));
                }

                Result.Sort((x, y) => y.Item2.CompareTo(x.Item2));
                
                List<string> sfile = new List<string>();
                foreach (Tuple<string, DateTime> t in Result)
                {
                    sfile.Add(t.Item1);
                }

                listBox1.DataSource = sfile;
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //deserialize
            byte[] ba = File.ReadAllBytes(Path.Combine(res_path, listBox1.SelectedItem.ToString()));
            object ob = ConvertByteArrayToObject(ba);
            textBox2.Text = encr2.Decrypt(ob.ToString());
        }

        public static byte[] ConvertObjectToByteArray(object ob)
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, ob);
            return ms.ToArray();
        }

        public static object ConvertByteArrayToObject(byte[] ba)
        {
            BinaryFormatter bf = new BinaryFormatter();
            Stream stream = new MemoryStream(ba);
            return bf.Deserialize(stream);
        }

        private void btnEncrypt_Click(object sender, EventArgs e)
        {
            string[] files = Directory.GetFiles(Environment.CurrentDirectory, "*.xml");
            foreach (string file in files)
            {
                byte[] ba = ConvertObjectToByteArray(encr2.Encrypt(File.ReadAllText(file)));
                File.WriteAllBytes(file+".caf.bin", ba);
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (textBox1.Text == "perinatal is the best i suppose")
            {
                btnEncrypt.Visible = true;
            }
            else
            {
                btnEncrypt.Visible = false;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (USER_AN.Where(x => x >= 0).ToList().Count()>0)
            {
                DialogResult dr = MessageBox.Show("Вы нажали на кнопку выхода из программы, не завершив тест! Вы действительно хотите выйти?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dr == DialogResult.Yes)
                {
                    button2_Click(sender, e);
                }

                if (dr == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            con_del();
            if (pictureBox11.Visible == true)
            pictureBox11.Visible = false;

            var senderGrid = (DataGridView)sender;

            if (senderGrid.Columns[e.ColumnIndex] is DataGridViewButtonColumn &&
                e.RowIndex >= 0)
            {
                    label1.Text = QW[e.RowIndex];

                    RadioButton RadioButton;
                    for (int j = 0; j < ANS[e.RowIndex].Count; j++)
                    {
                        RadioButton = new RadioButton
                        {
                            Location = new Point(6, 251 + (j * 30)),
                            Size = new Size(85, 17),
                            Name = "RadioButton" + j.ToString(),
                            AutoSize = true,
                            Text = ANS[e.RowIndex][j],
                        };
                        RadioButton.Click += new EventHandler(radioButton1_Click);
                        tabControl1.SelectedTab.Controls.Add(RadioButton);
                    }

                if (USER_AN[e.RowIndex] >= 0)
                {
                        RadioButton RadioButton1 = (RadioButton)Controls.Find("RadioButton" + USER_AN[e.RowIndex].ToString(), true).FirstOrDefault();
                        RadioButton1.Checked = true;
                }

                if (label1.Text.Contains(".jpg"))
                {
                    pictureBox11.Visible = true;
                    Bitmap bitmap = new Bitmap(Path.Combine(img_path, label1.Text));
                    pictureBox11.Image = bitmap;
                }

                //MessageBox.Show(USER_AN.Where(x => x < 0).ToList().Count().ToString());
                //if (USER_AN.Where(x => x < 0).ToList().Count() == 0)
                //{
                //    button2_Click(sender, e);
                //}
            }
        }
    }
}
